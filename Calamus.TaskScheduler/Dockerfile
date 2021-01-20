FROM mcr.microsoft.com/dotnet/aspnet:5.0-buster-slim
COPY . /quartznet
WORKDIR quartznet
# 时区设置
RUN ln -sf /usr/share/zoneinfo/Asia/Shanghai /etc/localtime
RUN echo 'Asia/Shanghai' >/etc/timezone
EXPOSE 80
ENTRYPOINT ["dotnet","Calamus.TaskScheduler.dll"]