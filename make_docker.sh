git pull
echo "Enter the password for davepl\'s Docker Hub:"
docker login -u davepl 

# Build and push a multi-arch image for amd64 and arm64
docker buildx build -t davepl/nightdriverserver:latest --platform linux/amd64,linux/arm64 --push .
# .NET 6.0 doesn't run on arm32 within QEMU, so we need to build and push that separately
docker build -t davepl/nightdriverserver:latest-arm32 -f Dockerfile.arm32 .
docker push davepl/nightdriverserver:latest-arm32


