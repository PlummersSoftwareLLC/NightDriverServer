git pull
echo "Enter the password for davepl\'s Docker Hub:"
docker login -u davepl 

# Build and push a multi-arch image for amd64 and arm64
docker buildx build -t davepl/nightdriverserver:latest --platform linux/amd64,linux/arm64 --push .


