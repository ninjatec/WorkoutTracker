#!/usr/bin/env python3
"""
SQL Server Credential Rotation Script

This script rotates SQL Server credentials stored in HashiCorp Vault and referenced
as Kubernetes external secrets. It ensures zero downtime by creating new credentials
before removing old ones and coordinating with the application's rolling update process.

Usage:
    python rotate-sql-credentials.py --vault-url <url> --vault-token <token> 
                                   --secret-path <path> --sql-server <server>
                                   [--dry-run] [--rollback]

Requirements:
    - hvac (HashiCorp Vault client)
    - pyodbc (SQL Server connection)
    - kubernetes (K8s client)
    - cryptography (password generation)
"""

import argparse
import logging
import os
import sys
import time
import json
import re
from datetime import datetime, timedelta
from typing import Dict, Optional, Tuple
import secrets
import string

try:
    import hvac
    import pyodbc
    from kubernetes import client, config
    from cryptography.fernet import Fernet
except ImportError as e:
    print(f"Missing required dependency: {e}")
    print("Install with: pip install hvac pyodbc kubernetes cryptography")
    sys.exit(1)

# Configure logging
log_file_path = '/tmp/sql-credential-rotation.log'
logging.basicConfig(
    level=logging.INFO,
    format='%(asctime)s - %(levelname)s - %(message)s',
    handlers=[
        logging.FileHandler(log_file_path),
        logging.StreamHandler()
    ]
)

# Set secure permissions on log file
try:
    import stat
    os.chmod(log_file_path, stat.S_IRUSR | stat.S_IWUSR)  # 600 permissions
except (OSError, ImportError):
    pass  # May not exist yet or on Windows
logger = logging.getLogger(__name__)

class SQLCredentialRotator:
    def __init__(self, vault_url: str, vault_token: str, secret_path: str, 
                 sql_server: str, database: str = "WorkoutTrackerWeb"):
        self.vault_url = vault_url
        self.vault_token = vault_token
        self.secret_path = secret_path
        self.sql_server = sql_server
        self.database = database
        self.vault_client = None
        self.k8s_client = None
        self.current_credentials = None
        self.new_credentials = None
        
    def initialize_clients(self):
        """Initialize Vault and Kubernetes clients"""
        try:
            # Initialize Vault client
            self.vault_client = hvac.Client(url=self.vault_url, token=self.vault_token)
            if not self.vault_client.is_authenticated():
                raise Exception("Failed to authenticate with Vault")
            
            # Initialize Kubernetes client
            try:
                config.load_incluster_config()
            except:
                config.load_kube_config()
            self.k8s_client = client.CoreV1Api()
            
            logger.info("Successfully initialized Vault and Kubernetes clients")
            
        except Exception as e:
            logger.error(f"Failed to initialize clients: {e}")
            raise

    def parse_connection_string(self, conn_str: str) -> Dict[str, str]:
        """Parse SQL Server connection string into components"""
        components = {}
        
        # Common connection string patterns
        patterns = {
            'server': r'Server=([^;]+)',
            'database': r'Database=([^;]+)',
            'user_id': r'User ID=([^;]+)',
            'password': r'Password=([^;]+)',
            'trust_cert': r'TrustServerCertificate=([^;]+)',
            'integrated_security': r'integrated security=([^;]+)'
        }
        
        for key, pattern in patterns.items():
            match = re.search(pattern, conn_str, re.IGNORECASE)
            if match:
                components[key] = match.group(1)
                
        return components

    def build_connection_string(self, components: Dict[str, str]) -> str:
        """Build connection string from components"""
        conn_str = f"Server={components['server']};Database={components['database']}"
        conn_str += f";TrustServerCertificate={components.get('trust_cert', 'True')}"
        conn_str += f";integrated security={components.get('integrated_security', 'False')}"
        conn_str += f";User ID={components['user_id']};Password={components['password']}"
        
        return conn_str

    def generate_secure_password(self, length: int = 16) -> str:
        """Generate a secure password for SQL Server"""
        # SQL Server password requirements:
        # - At least 8 characters
        # - Must contain characters from at least 3 of these 4 categories:
        #   - Uppercase letters
        #   - Lowercase letters  
        #   - Numbers
        #   - Special characters
        
        uppercase = string.ascii_uppercase
        lowercase = string.ascii_lowercase
        digits = string.digits
        special = "!@#$%^&*"
        
        # Ensure at least one character from each category
        password = [
            secrets.choice(uppercase),
            secrets.choice(lowercase),
            secrets.choice(digits),
            secrets.choice(special)
        ]
        
        # Fill the rest with random characters from all categories
        all_chars = uppercase + lowercase + digits + special
        for _ in range(length - 4):
            password.append(secrets.choice(all_chars))
            
        # Shuffle the password
        secrets.SystemRandom().shuffle(password)
        
        return ''.join(password)

    def get_current_credentials(self) -> Dict[str, str]:
        """Retrieve current credentials from Vault"""
        try:
            response = self.vault_client.secrets.kv.v2.read_secret_version(
                path=self.secret_path
            )
            
            connection_string = response['data']['data']['ConnectionStrings__WorkoutTrackerWebContext']
            credentials = self.parse_connection_string(connection_string)
            
            logger.info("Successfully retrieved current credentials from Vault")
            return credentials
            
        except Exception as e:
            logger.error(f"Failed to retrieve current credentials: {e}")
            raise

    def test_sql_connection(self, credentials: Dict[str, str]) -> bool:
        """Test SQL Server connection with given credentials"""
        try:
            conn_str = self.build_connection_string(credentials)
            conn = pyodbc.connect(conn_str, timeout=10)
            
            # Test with a simple query
            cursor = conn.cursor()
            cursor.execute("SELECT 1")
            cursor.fetchone()
            cursor.close()
            conn.close()
            
            logger.info("SQL connection test successful")
            return True
            
        except Exception as e:
            logger.error(f"SQL connection test failed: {e}")
            return False

    def create_new_sql_user(self, current_creds: Dict[str, str], new_username: str, new_password: str) -> bool:
        """Create new SQL Server user with same permissions as current user"""
        try:
            # Input validation for security
            if not re.match(r'^[a-zA-Z0-9_.@-]+$', new_username):
                raise ValueError(f"Invalid username format: {new_username}")
            
            if len(new_password) < 8 or len(new_password) > 128:
                raise ValueError("Password length must be between 8 and 128 characters")
            
            # Escape single quotes in password and username to prevent SQL injection
            escaped_password = new_password.replace("'", "''")
            escaped_username = new_username.replace("'", "''")
            escaped_current_user = current_creds['user_id'].replace("'", "''")
            escaped_database = self.database.replace("'", "''")
            
            conn_str = self.build_connection_string(current_creds)
            conn = pyodbc.connect(conn_str, timeout=30)
            cursor = conn.cursor()
            
            # Create new login with proper escaping
            create_login_sql = f"""
            IF NOT EXISTS (SELECT * FROM sys.server_principals WHERE name = '{escaped_username}')
            BEGIN
                CREATE LOGIN [{new_username}] WITH PASSWORD = '{escaped_password}';
            END
            """
            cursor.execute(create_login_sql)
            
            # Create user in database with proper escaping
            create_user_sql = f"""
            USE [{escaped_database}];
            IF NOT EXISTS (SELECT * FROM sys.database_principals WHERE name = '{escaped_username}')
            BEGIN
                CREATE USER [{new_username}] FOR LOGIN [{new_username}];
            END
            """
            cursor.execute(create_user_sql)
            
            # Get current user's roles and permissions with proper escaping
            get_roles_sql = f"""
            USE [{escaped_database}];
            SELECT r.name as role_name
            FROM sys.database_role_members rm
            JOIN sys.database_principals r ON rm.role_principal_id = r.principal_id
            JOIN sys.database_principals u ON rm.member_principal_id = u.principal_id
            WHERE u.name = '{escaped_current_user}';
            """
            cursor.execute(get_roles_sql)
            roles = [row[0] for row in cursor.fetchall()]
            
            # Add new user to same roles with proper escaping
            for role in roles:
                escaped_role = role.replace("'", "''")
                add_role_sql = f"USE [{escaped_database}]; ALTER ROLE [{escaped_role}] ADD MEMBER [{new_username}];"
                cursor.execute(add_role_sql)
            
            # Copy explicit permissions with proper escaping
            copy_permissions_sql = f"""
            USE [{escaped_database}];
            DECLARE @sql NVARCHAR(MAX) = '';
            SELECT @sql = @sql + 
                'GRANT ' + p.permission_name + 
                ' ON ' + p.class_desc + '::' + 
                CASE p.class
                    WHEN 0 THEN 'DATABASE'
                    WHEN 1 THEN OBJECT_SCHEMA_NAME(p.major_id) + '.' + OBJECT_NAME(p.major_id)
                    ELSE 'UNKNOWN'
                END + 
                ' TO [{new_username}];' + CHAR(13)
            FROM sys.database_permissions p
            JOIN sys.database_principals u ON p.grantee_principal_id = u.principal_id
            WHERE u.name = '{escaped_current_user}' AND p.state = 'G';
            
            EXEC sp_executesql @sql;
            """
            cursor.execute(copy_permissions_sql)
            
            conn.commit()
            cursor.close()
            conn.close()
            
            logger.info(f"Successfully created new SQL user: {new_username}")
            return True
            
        except Exception as e:
            logger.error(f"Failed to create new SQL user: {e}")
            return False

    def update_vault_secret(self, new_credentials: Dict[str, str], backup_old: bool = True) -> bool:
        """Update credentials in Vault with optional backup"""
        try:
            if backup_old:
                # Create backup of current secret
                timestamp = datetime.now().strftime("%Y%m%d_%H%M%S")
                backup_path = f"{self.secret_path}_backup_{timestamp}"
                
                current_secret = self.vault_client.secrets.kv.v2.read_secret_version(
                    path=self.secret_path
                )
                
                self.vault_client.secrets.kv.v2.create_or_update_secret(
                    path=backup_path,
                    secret=current_secret['data']['data']
                )
                logger.info(f"Created backup at: {backup_path}")
            
            # Update with new credentials
            current_secret = self.vault_client.secrets.kv.v2.read_secret_version(
                path=self.secret_path
            )
            
            updated_data = current_secret['data']['data'].copy()
            new_conn_str = self.build_connection_string(new_credentials)
            
            # Update both connection string keys
            updated_data['ConnectionStrings__WorkoutTrackerWebContext'] = new_conn_str
            updated_data['ConnectionStrings__DefaultConnection'] = new_conn_str
            
            self.vault_client.secrets.kv.v2.create_or_update_secret(
                path=self.secret_path,
                secret=updated_data
            )
            
            logger.info("Successfully updated Vault secret")
            return True
            
        except Exception as e:
            logger.error(f"Failed to update Vault secret: {e}")
            return False

    def trigger_external_secret_refresh(self, namespace: str = "default", 
                                      external_secret_name: str = "workouttracker-secrets") -> bool:
        """Trigger External Secret Operator to refresh the secret"""
        try:
            # Get the external secret
            custom_api = client.CustomObjectsApi()
            
            external_secret = custom_api.get_namespaced_custom_object(
                group="external-secrets.io",
                version="v1beta1",
                namespace=namespace,
                plural="externalsecrets",
                name=external_secret_name
            )
            
            # Force refresh by updating annotation
            if 'metadata' not in external_secret:
                external_secret['metadata'] = {}
            if 'annotations' not in external_secret['metadata']:
                external_secret['metadata']['annotations'] = {}
                
            external_secret['metadata']['annotations']['force-sync'] = str(int(time.time()))
            
            custom_api.patch_namespaced_custom_object(
                group="external-secrets.io",
                version="v1beta1",
                namespace=namespace,
                plural="externalsecrets",
                name=external_secret_name,
                body=external_secret
            )
            
            logger.info("Triggered external secret refresh")
            return True
            
        except Exception as e:
            logger.error(f"Failed to trigger external secret refresh: {e}")
            return False

    def wait_for_pods_ready(self, namespace: str = "default", 
                           app_label: str = "workouttracker", timeout: int = 300) -> bool:
        """Wait for all application pods to be ready after credential rotation"""
        try:
            start_time = time.time()
            
            while time.time() - start_time < timeout:
                pods = self.k8s_client.list_namespaced_pod(
                    namespace=namespace,
                    label_selector=f"app={app_label}"
                )
                
                ready_count = 0
                total_count = len(pods.items)
                
                for pod in pods.items:
                    if pod.status.phase == "Running":
                        ready_conditions = [
                            condition for condition in pod.status.conditions 
                            if condition.type == "Ready" and condition.status == "True"
                        ]
                        if ready_conditions:
                            ready_count += 1
                
                if ready_count == total_count and total_count > 0:
                    logger.info(f"All {total_count} pods are ready")
                    return True
                
                logger.info(f"Waiting for pods: {ready_count}/{total_count} ready")
                time.sleep(10)
            
            logger.error(f"Timeout waiting for pods to be ready")
            return False
            
        except Exception as e:
            logger.error(f"Error waiting for pods: {e}")
            return False

    def cleanup_old_sql_user(self, old_username: str) -> bool:
        """Remove old SQL Server user after successful rotation"""
        try:
            new_conn_str = self.build_connection_string(self.new_credentials)
            conn = pyodbc.connect(new_conn_str, timeout=30)
            cursor = conn.cursor()
            
            # Drop user from database
            drop_user_sql = f"""
            USE [{self.database}];
            IF EXISTS (SELECT * FROM sys.database_principals WHERE name = ?)
            BEGIN
                DROP USER [?];
            END
            """
            cursor.execute(drop_user_sql, (old_username, old_username))
            
            # Drop login
            drop_login_sql = """
            IF EXISTS (SELECT * FROM sys.server_principals WHERE name = ?)
            BEGIN
                DROP LOGIN [?];
            END
            """
            cursor.execute(drop_login_sql, (old_username, old_username))
            
            conn.commit()
            cursor.close()
            conn.close()
            
            logger.info(f"Successfully removed old SQL user: {old_username}")
            return True
            
        except Exception as e:
            logger.error(f"Failed to cleanup old SQL user: {e}")
            return False

    def rollback_credentials(self, backup_timestamp: str) -> bool:
        """Rollback to previous credentials using backup"""
        try:
            backup_path = f"{self.secret_path}_backup_{backup_timestamp}"
            
            # Get backup secret
            backup_secret = self.vault_client.secrets.kv.v2.read_secret_version(
                path=backup_path
            )
            
            # Restore to main secret path
            self.vault_client.secrets.kv.v2.create_or_update_secret(
                path=self.secret_path,
                secret=backup_secret['data']['data']
            )
            
            logger.info(f"Successfully rolled back to backup: {backup_timestamp}")
            return True
            
        except Exception as e:
            logger.error(f"Failed to rollback credentials: {e}")
            return False

    def rotate_credentials(self, dry_run: bool = False) -> bool:
        """Main credential rotation process"""
        try:
            logger.info("Starting SQL credential rotation process")
            
            # Step 1: Get current credentials
            self.current_credentials = self.get_current_credentials()
            current_username = self.current_credentials['user_id']
            
            # Step 2: Generate new credentials
            timestamp = datetime.now().strftime("%Y%m%d%H%M")
            new_username = f"{current_username.split('.')[0]}.{timestamp}"
            new_password = self.generate_secure_password()
            
            self.new_credentials = self.current_credentials.copy()
            self.new_credentials['user_id'] = new_username
            self.new_credentials['password'] = new_password
            
            logger.info(f"Generated new credentials for user: {new_username}")
            
            if dry_run:
                logger.info("DRY RUN: Would proceed with credential rotation")
                return True
            
            # Step 3: Test current connection
            if not self.test_sql_connection(self.current_credentials):
                raise Exception("Current credentials are not working")
            
            # Step 4: Create new SQL user
            if not self.create_new_sql_user(self.current_credentials, new_username, new_password):
                raise Exception("Failed to create new SQL user")
            
            # Step 5: Test new credentials
            if not self.test_sql_connection(self.new_credentials):
                raise Exception("New credentials are not working")
            
            # Step 6: Update Vault secret
            if not self.update_vault_secret(self.new_credentials):
                raise Exception("Failed to update Vault secret")
            
            # Step 7: Trigger external secret refresh
            if not self.trigger_external_secret_refresh():
                logger.warning("Failed to trigger external secret refresh - may need manual intervention")
            
            # Step 8: Wait for pods to restart and be ready
            logger.info("Waiting for application pods to restart with new credentials...")
            if not self.wait_for_pods_ready():
                logger.warning("Pods may not have restarted properly - verify manually")
            
            # Step 9: Cleanup old user
            time.sleep(30)  # Give some buffer time
            if not self.cleanup_old_sql_user(current_username):
                logger.warning(f"Failed to cleanup old user {current_username} - manual cleanup required")
            
            logger.info("SQL credential rotation completed successfully")
            return True
            
        except Exception as e:
            logger.error(f"Credential rotation failed: {e}")
            return False

def main():
    parser = argparse.ArgumentParser(description="Rotate SQL Server credentials")
    parser.add_argument("--vault-url", required=True, help="Vault server URL")
    parser.add_argument("--vault-token", required=True, help="Vault authentication token")
    parser.add_argument("--secret-path", required=True, help="Path to secret in Vault")
    parser.add_argument("--sql-server", required=True, help="SQL Server hostname/IP")
    parser.add_argument("--database", default="WorkoutTrackerWeb", help="Database name")
    parser.add_argument("--namespace", default="default", help="Kubernetes namespace")
    parser.add_argument("--app-label", default="workouttracker", help="App label for pod selection")
    parser.add_argument("--dry-run", action="store_true", help="Perform dry run without changes")
    parser.add_argument("--rollback", help="Rollback to backup timestamp (YYYYMMDD_HHMMSS)")
    
    args = parser.parse_args()
    
    # Initialize rotator
    rotator = SQLCredentialRotator(
        vault_url=args.vault_url,
        vault_token=args.vault_token,
        secret_path=args.secret_path,
        sql_server=args.sql_server,
        database=args.database
    )
    
    try:
        rotator.initialize_clients()
        
        if args.rollback:
            success = rotator.rollback_credentials(args.rollback)
        else:
            success = rotator.rotate_credentials(dry_run=args.dry_run)
        
        if success:
            logger.info("Operation completed successfully")
            sys.exit(0)
        else:
            logger.error("Operation failed")
            sys.exit(1)
            
    except Exception as e:
        logger.error(f"Fatal error: {e}")
        sys.exit(1)

if __name__ == "__main__":
    main()
