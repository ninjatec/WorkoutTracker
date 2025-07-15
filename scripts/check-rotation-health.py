#!/usr/bin/env python3
"""
SQL Credential Rotation Health Check Script

This script monitors the health of the credential rotation system by checking:
- Last successful rotation timestamp
- Credential age
- System readiness for rotation
- Vault connectivity
- SQL Server connectivity
- Kubernetes access

Usage:
    python check-rotation-health.py --vault-url <url> --vault-token <token> --secret-path <path>
"""

import argparse
import json
import logging
import re
import sys
from datetime import datetime, timedelta
from typing import Dict, List, Optional

try:
    import hvac
    import pyodbc
    from kubernetes import client, config
except ImportError as e:
    print(f"Missing required dependency: {e}")
    print("Install with: pip install hvac pyodbc kubernetes")
    sys.exit(1)

logging.basicConfig(level=logging.INFO, format='%(asctime)s - %(levelname)s - %(message)s')
logger = logging.getLogger(__name__)

class RotationHealthChecker:
    def __init__(self, vault_url: str, vault_token: str, secret_path: str, sql_server: str):
        self.vault_url = vault_url
        self.vault_token = vault_token
        self.secret_path = secret_path
        self.sql_server = sql_server
        self.issues = []
        self.warnings = []

        self.redacted_values = {
            self.secret_path: "[REDACTED]",
            self.vault_token: "[REDACTED]",
            self.sql_server: "[REDACTED]",
        }
        
    def add_issue(self, issue: str):
        """Add a critical issue"""
        sanitized_issue = self._sanitize_message(issue)
        self.issues.append(sanitized_issue)
        logger.error(f"ISSUE: {sanitized_issue}")
        
    def add_warning(self, warning: str):
        """Add a warning"""

        sanitized_warning = self._sanitize_message(warning)
        self.warnings.append(sanitized_warning)
        logger.warning(f"WARNING: {sanitized_warning}")
        
    def check_vault_connectivity(self) -> bool:
        """Check if Vault is accessible and token is valid"""
        try:
            client = hvac.Client(url=self.vault_url, token=self.vault_token)
            if not client.is_authenticated():
                self.add_issue("Vault authentication failed")
                return False
                
            # Try to read the secret
            response = client.secrets.kv.v2.read_secret_version(path=self.secret_path)
            if not response:
                self.add_issue(f"Cannot read secret at path: {self.secret_path}")
                return False
                
            logger.info("Vault connectivity: OK")
            return True
            
        except Exception as e:
            self.add_issue(f"Vault connectivity failed: {e}")
            return False
            
    def check_sql_connectivity(self) -> bool:
        """Check SQL Server connectivity with current credentials"""
        try:
            # Get current credentials from Vault
            vault_client = hvac.Client(url=self.vault_url, token=self.vault_token)
            response = vault_client.secrets.kv.v2.read_secret_version(path=self.secret_path)
            
            conn_str = response['data']['data']['ConnectionStrings__WorkoutTrackerWebContext']
            
            # Test connection
            conn = pyodbc.connect(conn_str, timeout=10)
            cursor = conn.cursor()
            cursor.execute("SELECT 1")
            cursor.fetchone()
            cursor.close()
            conn.close()
            
            logger.info("SQL Server connectivity: OK")
            return True
            
        except Exception as e:
            self.add_issue(f"SQL Server connectivity failed: {e}")
            return False
            
    def check_kubernetes_access(self) -> bool:
        """Check Kubernetes API access"""
        try:
            try:
                config.load_incluster_config()
            except:
                config.load_kube_config()
                
            v1 = client.CoreV1Api()
            
            # Test basic access
            v1.list_pod_for_all_namespaces(limit=1)
            
            # Test External Secrets access
            custom_api = client.CustomObjectsApi()
            try:
                custom_api.list_cluster_custom_object(
                    group="external-secrets.io",
                    version="v1beta1",
                    plural="externalsecrets",
                    limit=1
                )
            except:
                self.add_warning("Cannot access External Secrets - may need manual secret refresh")
            
            logger.info("Kubernetes access: OK")
            return True
            
        except Exception as e:
            self.add_issue(f"Kubernetes access failed: {e}")
            return False
            
    def check_credential_age(self) -> Optional[dict]:
        """Check the age of current credentials"""
        try:
            vault_client = hvac.Client(url=self.vault_url, token=self.vault_token)
            response = vault_client.secrets.kv.v2.read_secret_version(path=self.secret_path)
            
            conn_str = response['data']['data']['ConnectionStrings__WorkoutTrackerWebContext']
            
            # Parse username to extract timestamp if present
            import re
            user_match = re.search(r'User ID=([^;]+)', conn_str)
            if not user_match:
                self.add_warning("Cannot parse username from connection string")
                return None
                
            username = user_match.group(1)
            
            # Check if username has timestamp format (user.YYYYMMDDHHMM)
            timestamp_match = re.search(r'\.(\d{12})$', username)
            if timestamp_match:
                timestamp_str = timestamp_match.group(1)
                try:
                    created_date = datetime.strptime(timestamp_str, '%Y%m%d%H%M')
                    age_days = (datetime.now() - created_date).days
                    
                    credential_info = {
                        'username': username,
                        'created_date': created_date.isoformat(),
                        'age_days': age_days
                    }
                    
                    if age_days > 45:
                        self.add_warning(f"Credentials are {age_days} days old - consider rotation")
                    elif age_days > 30:
                        self.add_warning(f"Credentials are {age_days} days old - rotation due soon")
                    else:
                        logger.info(f"Credential age: {age_days} days (OK)")
                        
                    return credential_info
                    
                except ValueError:
                    self.add_warning(f"Cannot parse timestamp from username: {username}")
                    
            else:
                self.add_warning(f"Username does not contain timestamp: {username}")
                
            return None
            
        except Exception as e:
            self.add_warning(f"Cannot check credential age: {e}")
            return None
            
    def check_backup_retention(self) -> List[str]:
        """Check for existing credential backups"""
        try:
            vault_client = hvac.Client(url=self.vault_url, token=self.vault_token)
            
            # List secrets to find backups
            try:
                # This requires list permission on the secrets path
                base_path = '/'.join(self.secret_path.split('/')[:-1])
                response = vault_client.secrets.kv.v2.list_secrets(path=base_path)
                
                backup_keys = []
                secret_name = self.secret_path.split('/')[-1]
                
                for key in response['data']['keys']:
                    if key.startswith(f"{secret_name}_backup_"):
                        backup_keys.append(key)
                        
                if backup_keys:
                    logger.info(f"Found {len(backup_keys)} backup(s)")
                    
                    # Check if backups are too old
                    old_backups = []
                    for backup in backup_keys:
                        timestamp_match = re.search(r'_backup_(\d{8}_\d{6})$', backup)
                        if timestamp_match:
                            timestamp_str = timestamp_match.group(1)
                            try:
                                backup_date = datetime.strptime(timestamp_str, '%Y%m%d_%H%M%S')
                                age_days = (datetime.now() - backup_date).days
                                if age_days > 90:  # Keep backups for 90 days
                                    old_backups.append(backup)
                            except ValueError:
                                pass
                                
                    if old_backups:
                        self.add_warning(f"Found {len(old_backups)} old backup(s) that could be cleaned up")
                        
                else:
                    self.add_warning("No credential backups found")
                    
                return backup_keys
                
            except Exception as e:
                self.add_warning(f"Cannot list backups (may not have list permission): {e}")
                return []
                
        except Exception as e:
            self.add_warning(f"Cannot check backup retention: {e}")
            return []
            
    def check_rotation_schedule(self) -> Optional[dict]:
        """Check if automated rotation is scheduled"""
        try:
            try:
                config.load_incluster_config()
            except:
                config.load_kube_config()
                
            batch_api = client.BatchV1Api()
            
            # Look for credential rotation CronJob
            cronjobs = batch_api.list_cron_job_for_all_namespaces()
            
            rotation_cronjobs = []
            for cronjob in cronjobs.items:
                if 'credential' in cronjob.metadata.name.lower() and 'rotation' in cronjob.metadata.name.lower():
                    rotation_cronjobs.append({
                        'name': cronjob.metadata.name,
                        'namespace': cronjob.metadata.namespace,
                        'schedule': cronjob.spec.schedule,
                        'suspended': cronjob.spec.suspend,
                        'last_schedule': cronjob.status.last_schedule_time.isoformat() if cronjob.status.last_schedule_time else None
                    })
                    
            if rotation_cronjobs:
                logger.info(f"Found {len(rotation_cronjobs)} rotation CronJob(s)")
                for job in rotation_cronjobs:
                    if job['suspended']:
                        self.add_warning(f"CronJob {job['name']} is suspended")
            else:
                self.add_warning("No automated rotation CronJob found")
                
            return rotation_cronjobs
            
        except Exception as e:
            self.add_warning(f"Cannot check rotation schedule: {e}")
            return None
            
    def generate_report(self) -> dict:
        """Generate comprehensive health report"""
        logger.info("Starting credential rotation health check...")
        
        report = {
            'timestamp': datetime.now().isoformat(),
            'vault_connectivity': False,
            'sql_connectivity': False,
            'kubernetes_access': False,
            'credential_info': None,
            'backups': [],
            'rotation_schedule': None,
            'issues': [],
            'warnings': [],
            'overall_health': 'UNKNOWN'
        }
        
        # Run all checks
        report['vault_connectivity'] = self.check_vault_connectivity()
        report['sql_connectivity'] = self.check_sql_connectivity()
        report['kubernetes_access'] = self.check_kubernetes_access()
        report['credential_info'] = self.check_credential_age()
        report['backups'] = self.check_backup_retention()
        report['rotation_schedule'] = self.check_rotation_schedule()
        
        # Compile issues and warnings
        report['issues'] = self.issues
        report['warnings'] = self.warnings
        
        # Determine overall health
        if self.issues:
            report['overall_health'] = 'CRITICAL'
        elif self.warnings:
            report['overall_health'] = 'WARNING'
        else:
            report['overall_health'] = 'HEALTHY'
            
        return report
        
    def print_summary(self, report: dict):
        """Print human-readable summary"""
        print("\n" + "="*60)
        print("SQL CREDENTIAL ROTATION HEALTH CHECK")
        print("="*60)
        
        print(f"\nOverall Health: {report['overall_health']}")
        print(f"Check Time: {report['timestamp']}")
        
        print(f"\nConnectivity Checks:")
        print(f"  Vault:      {'✓' if report['vault_connectivity'] else '✗'}")
        print(f"  SQL Server: {'✓' if report['sql_connectivity'] else '✗'}")
        print(f"  Kubernetes: {'✓' if report['kubernetes_access'] else '✗'}")
        
        if report['credential_info']:
            cred = report['credential_info']
            print(f"\nCredential Information:")
            print(f"  Username: {cred['username']}")
            print(f"  Age: {cred['age_days']} days")
            print(f"  Created: {cred['created_date']}")
            
        if report['backups']:
            print(f"\nBackups: {len(report['backups'])} found")
            
        if report['rotation_schedule']:
            print(f"\nScheduled Rotation:")
            for job in report['rotation_schedule']:
                status = "Active" if not job['suspended'] else "Suspended"
                print(f"  {job['name']}: {job['schedule']} ({status})")
                
        if report['issues']:
            print(f"\n🚨 CRITICAL ISSUES ({len(report['issues'])}):")
            for issue in report['issues']:
                print(f"  • {issue}")
                
        if report['warnings']:
            print(f"\n⚠️  WARNINGS ({len(report['warnings'])}):")
            for warning in report['warnings']:
                print(f"  • {warning}")
                
        if report['overall_health'] == 'HEALTHY':
            print(f"\n✅ System is healthy and ready for credential rotation!")
            
        print("\n" + "="*60)

def main():
    parser = argparse.ArgumentParser(description="Check credential rotation system health")
    parser.add_argument("--vault-url", required=True, help="Vault server URL")
    parser.add_argument("--vault-token", required=True, help="Vault authentication token")
    parser.add_argument("--secret-path", required=True, help="Path to secret in Vault")
    parser.add_argument("--sql-server", required=True, help="SQL Server hostname/IP")
    parser.add_argument("--json", action="store_true", help="Output in JSON format")
    parser.add_argument("--exit-code", action="store_true", help="Exit with non-zero code on issues")
    
    args = parser.parse_args()
    
    checker = RotationHealthChecker(
        vault_url=args.vault_url,
        vault_token=args.vault_token,
        secret_path=args.secret_path,
        sql_server=args.sql_server
    )
    
    report = checker.generate_report()
    
    if args.json:
        print(json.dumps(report, indent=2))
    else:
        checker.print_summary(report)
        
    if args.exit_code:
        if report['overall_health'] == 'CRITICAL':
            sys.exit(2)
        elif report['overall_health'] == 'WARNING':
            sys.exit(1)
        else:
            sys.exit(0)

if __name__ == "__main__":
    main()
