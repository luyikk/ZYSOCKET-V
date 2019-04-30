# ZYSOCKET 第5代异构框架

## NUGET
## Server: PM> Install-Package ZYSocketServerV
## Client:  PM> Install-Package ZYSocketClientV
### 推荐IKende很好用的TCP测试工具: https://github.com/IKende/TCPBenchmarks
## 关于性能测试 ECHO:
##### 使用TCPBenchmarks 测试 CPU  E1230V2  10G网的情况下,可达到37W-38.5W RPS左右
## 特性介绍:
#### 完全采用 async await模拟fiber编写
#### 性能优异，稳定性极高
#### 内存占用量非常小 1000连接量只需4M(4096Bx1000/1024=4M,默认4096B数据缓冲区,可以设置成最小1B)内存开支 
#### 没有额外数据缓冲区，用来处理粘包等问题
#### 支持所有平台，不仅仅是.net core，包括.net fx,mono
#### 支持windwos linux ios android。因为不会有emit这样的代码生成所以完美支持IOS
#### 支持stream嵌套操作
#### 支持SSL协议，和各种压缩流操作
#### 框架简洁优雅，可参考各种DEMO，入门非常简单
#### 支持传统的数据包格式 len+data
#### 或者await 即读即用模式 直接read data->user data->next read data...

![](https://github.com/luyikk/ZYSOCKET-V/blob/master/Benchmarks/echoBenchmarks.png?raw=true)
