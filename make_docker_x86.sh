git pull
docker build . -t nightdriver -f Dockerfile.x86
docker tag nightdriver davepl/nightdriverserver:latest
echo "Enter the password for davepl\'s Docker Hub:"
docker login -u davepl 
docker push davepl/nightdriverserver:latest

