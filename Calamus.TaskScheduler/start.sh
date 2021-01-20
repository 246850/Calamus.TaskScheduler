docker rmi quartznet1.0
docker build --no-cache -t quartznet1.0 .
docker stop quartznet
docker rm quartznet
docker run --name quartznet --privileged=true --restart=always -d -p 1063:80 quartznet1.0
docker ps -a