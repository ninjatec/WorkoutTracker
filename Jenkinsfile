pipeline {
    agent {
        label 'linux'
    }
    
    parameters {
        choice(
            name: 'VERSION_TYPE',
            choices: ['build', 'patch', 'minor', 'major'],
            description: 'Type of version update to perform'
        )
    }
    
    environment {
        DOCKER_REPO = "ninjatec/workout-tracker"
        DOCKER_DEFAULT_PLATFORM = "linux/amd64"
        DOTNET_RUNTIME_IDENTIFIER = "linux-x64"
    }
    
    stages {
        stage('Setup') {
            steps {
                checkout scm
                sh 'git config --global user.email "jenkins@example.com"'
                sh 'git config --global user.name "Jenkins CI"'
            }
        }
        
        stage('Update Version') {
            steps {
                sh './scripts/update-version.sh ${params.VERSION_TYPE}'
                script {
                    def versionJson = readJSON file: 'version.json'
                    env.VERSION_MAJOR = versionJson.major
                    env.VERSION_MINOR = versionJson.minor
                    env.VERSION_PATCH = versionJson.patch
                    env.VERSION_BUILD = versionJson.build
                    env.VERSION = "${env.VERSION_MAJOR}.${env.VERSION_MINOR}.${env.VERSION_PATCH}.${env.VERSION_BUILD}"
                    env.GIT_COMMIT = sh(script: 'git rev-parse --short HEAD', returnStdout: true).trim()
                    
                    echo "Version set to ${env.VERSION}"
                    echo "Git commit: ${env.GIT_COMMIT}"
                }
            }
        }
                
        stage('Build Docker Image') {
            steps {
                sh """
                dotnet publish \\
                  --os linux \\
                  --arch x64 \\
                  /t:PublishContainer \\
                  -p:ContainerImageName=${env.DOCKER_REPO} \\
                  -p:ContainerImageTag=${env.VERSION} \\
                  -p:PublishTrimmed=false \\
                  -p:ContainerRegistry="" \\
                  -p:TargetArch=x64 \\
                  -p:TargetOS=linux \\
                  --configuration Release
                """
                
                script {
                    // Get built image name
                    def builtImageName = sh(
                        script: "docker images --format '{{.Repository}}:{{.Tag}}' | grep -E '${env.DOCKER_REPO}|workouttrackerweb' | head -n 1",
                        returnStdout: true
                    ).trim()
                    
                    env.BUILT_IMAGE_NAME = builtImageName
                    
                    echo "Found image: ${env.BUILT_IMAGE_NAME}"
                }
            }
        }
        
        stage('Tag Docker Images') {
            steps {
                sh "docker tag ${env.BUILT_IMAGE_NAME} ${env.DOCKER_REPO}:${env.VERSION}"
                sh "docker tag ${env.BUILT_IMAGE_NAME} ${env.DOCKER_REPO}:latest"
            }
        }
        
        stage('Push Docker Images') {
            steps {
                withCredentials([usernamePassword(credentialsId: 'docker-registry-credentials', usernameVariable: 'DOCKER_USERNAME', passwordVariable: 'DOCKER_PASSWORD')]) {
                    sh 'docker login -u $DOCKER_USERNAME -p $DOCKER_PASSWORD'
                }
                
                retry(3) {
                    sh "docker push ${env.DOCKER_REPO}:${env.VERSION}"
                }
                
                retry(2) {
                    sh "docker push ${env.DOCKER_REPO}:latest"
                }
            }
        }
        
        stage('Update Kubernetes Manifests') {
            steps {
                script {
                    // Update the image tag in deployment.yaml
                    sh """
                    sed -i "s|image: ${env.DOCKER_REPO}:.*|image: ${env.DOCKER_REPO}:${env.VERSION}|" ./k8s/deployment.yaml
                    sed -i "s|image: ${env.DOCKER_REPO}:.*|image: ${env.DOCKER_REPO}:${env.VERSION}|" ./k8s/hangfire-worker.yaml
                    """
                }
            }
        }
        
        stage('Update Database Version') {
            steps {
                sh """
                # Create the SQL file to update the version in the database
                cat > version_update.sql << EOF
-- Recording deployment version
INSERT INTO Deployments (Version, GitCommit, DeploymentDate)
VALUES ('${env.VERSION}', '${env.GIT_COMMIT}', CURRENT_TIMESTAMP);
EOF
                """
                
                sh './scripts/update-database-version.sh'
            }
        }
        
        stage('Commit Version Changes') {
            steps {
                sh """
                git add ./k8s/deployment.yaml
                git add ./k8s/hangfire-worker.yaml
                git add ./version.json
                git add ./version_update.sql
                git commit -m "Deploy version ${env.VERSION}"
                """
            }
        }
        
        stage('Push to Git Repository') {
            steps {
                withCredentials([sshUserPrivateKey(credentialsId: 'github-ssh-key', keyFileVariable: 'SSH_KEY')]) {
                    sh """
                    mkdir -p ~/.ssh
                    cp \$SSH_KEY ~/.ssh/id_rsa
                    chmod 600 ~/.ssh/id_rsa
                    ssh-keyscan -t rsa github.com >> ~/.ssh/known_hosts
                    git push origin HEAD:main
                    """
                }
            }
        }
    }
    
    post {
        success {
            echo """
=========================================================
  Deployment Complete!                               
  Version: ${env.VERSION}                                
  Commit: ${env.GIT_COMMIT}                              
=========================================================
            """
        }
        failure {
            echo "Deployment failed!"
            
            // Optional: Add notification system here (e.g., email, Slack)
            // slackSend channel: '#deployments', color: 'danger', message: "Deployment of WorkoutTracker v${env.VERSION} failed!"
        }
        always {
            // Clean up Docker images to avoid disk space issues
            sh "docker rmi ${env.DOCKER_REPO}:${env.VERSION} || true"
            sh "docker rmi ${env.DOCKER_REPO}:latest || true"
            sh "docker rmi ${env.BUILT_IMAGE_NAME} || true"
            
            // Clean workspace
            cleanWs()
        }
    }
}
