git pull
docker build . -t nightdriver -f Dockerfile.arm64
docker tag nightdriver davepl/nightdriverserver:arm64
echo "Enter the password for davepl\'s Docker Hub:"
docker login -u davepl
docker push davepl/nightdriverserver:arm64


