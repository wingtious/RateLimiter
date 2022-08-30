# RateLimiter

当你接入第三方应用且其接口有频率限制时,使用此项目.

注入:

```c#
services.AddRateLimiter<RedisRateLimiter>(TimeSpan.FromSeconds(1), 50, "10.45.11.168:6001,password=goatest@!$%");
```

参数:一定时间内,发送消息频率,如果使用RedisRateLimiter,需要提供 redis地址.

调用:

```c#
 _timeLimiter.Enqueue(() => ConsoleIt(i))
```

测试结果

![image-20220830182801732](C:\Users\bg458896\AppData\Roaming\Typora\typora-user-images\image-20220830182801732.png)

