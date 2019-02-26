# ZYSOCKET 第5代框架
完全采用 async await模拟fiber编写
速度绝对优越，可测试
内存占用量非常小 1000连接量只需5M内存开支 
没有额外数据缓冲区，用来处理粘包等问题
支持所有平台，不仅仅是.net core，包括.net fx,mono
支持windwos linux ios android。因为不会有emit这样的代码生成所以完美支持IOS
支持stream嵌套操作
支持SSL协议，和各种压缩流操作
框架简洁优雅，可参考各种DEMO，入门非常简单。
支持传统的数据包格式 len+data
或者await模式 直接read type

