git pull
docker build . -t nightdriver -f Dockerfile.arm32
docker tag nightdriver davepl/nightdriverserver:arm32
echo "Enter the password for davepl\'s Docker Hub:"
docker login -u davepl
docker push davepl/nightdriverserver:arm32


