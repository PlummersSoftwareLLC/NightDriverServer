git pull
docker build . -t nightdriver
docker tag nightdriver davepl/nightdriverweb:latest
echo "Enter the password for davepl\'s Docker Hub:"
docker login -u davepl
docker push davepl/nightdriverweb:latest

