# Calamus.TaskScheduler
基于Asp.Net Core 5.0采用Quartz.Net编写的开源任务调度Web管理平台

**演示站点**

http://47.101.47.193:1063/

**部署步骤**

### 1、创建持久化数据库（以MySQL为例）

> --创建数据库 quartz , Charset为utf8mb4

> --根据database/tables/tables_mysql_innodb.sql语句创建表结构

### 2、启动方式
> --docker部署（推荐）

> --命令行启动 dotnet Calamus.TaskScheduler.dll

> --IIS部署（不推荐）

**配置文件**
```
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning",
      "Microsoft.Hosting.Lifetime": "Information"
    }
  },
  "AllowedHosts": "*",
  "Quartz": {
    "Database": "server=localhost;port=3306;database=quartz;User ID=root;Password=123456", // quartz 持久化数据库连接，本实例使用MySQL
    "TablePrefix": "QRTZ_"  // MySQL下注意区分大小写
  }
}

```
