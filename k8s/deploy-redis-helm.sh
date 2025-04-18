#!/bin/bash
# Script to deploy Redis HA with Sentinel using Helm

# Set namespace
NAMESPACE="web"

# Check if namespace exists
if ! kubectl get namespace $NAMESPACE &> /dev/null; then
  echo "Creating namespace $NAMESPACE..."
  kubectl create namespace $NAMESPACE
fi

# Add Bitnami repo
echo "Adding Bitnami Helm repo..."
helm repo add bitnami https://charts.bitnami.com/bitnami
helm repo update

# Extract values file from ConfigMap
echo "Extracting Redis Helm values..."
kubectl apply -f redis-helm.yaml
kubectl get configmap redis-helm-values -n $NAMESPACE -o jsonpath="{.data.redis-values\.yaml}" > redis-values.yaml

# Check for existing Redis deployment
if helm list -n $NAMESPACE | grep -q "redis"; then
  echo "Updating existing Redis deployment..."
  helm upgrade redis bitnami/redis -f redis-values.yaml -n $NAMESPACE
else
  echo "Installing new Redis deployment..."
  helm install redis bitnami/redis -f redis-values.yaml -n $NAMESPACE
fi

# Cleanup
echo "Cleaning up temporary files..."
rm -f redis-values.yaml

# Wait for pods
echo "Waiting for Redis pods to be ready..."
kubectl wait --for=condition=ready pod -l app.kubernetes.io/name=redis -n $NAMESPACE --timeout=300s

echo "Redis HA with Sentinel has been deployed!"
echo "To connect to Redis:"
echo "  Master: redis-master.$NAMESPACE.svc.cluster.local:6379"
echo "  Replicas: redis-replicas.$NAMESPACE.svc.cluster.local:6379" 
echo "  Sentinel: redis.$NAMESPACE.svc.cluster.local:26379"