<!DOCTYPE html>
<html>

<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width, initial-scale=1.0">
  <title>README</title>
  <link rel="stylesheet" href="https://stackedit.io/style.css" />
</head>

<body class="stackedit">
  <div class="stackedit__html"><h1 id="zysocket-第5代异构框架zysocket-v">ZYSOCKET 第5代异构框架(ZYSOCKET-V)</h1>
<h2 id="使用方式">使用方式:</h2>
<p>NUGET:</p>
<p><a href="%28https://www.nuget.org/packages/ZYSocketServerV%29">Server link description here</a></p>
<pre><code>Server: PM&gt; Install-Package ZYSocketServerV
</code></pre>
<p><a href="https://www.nuget.org/packages/ZYSocketClientV">Client link description here</a></p>
<pre><code>Client: PM&gt; Install-Package ZYSocketClientV
</code></pre>
<h2 id="介绍">介绍:</h2>
<p><strong>构架:</strong><br>
采用PopStack的PIPE Read 模式,例如 读取 一个字符串</p>
<pre><code> var str=await fiberRw.ReadString(); 
 var value=await fiberRw.Readint32();
</code></pre>
<p>你不需要知道数据包缓冲好了没,读到了没,当它准确读取到这段数据后,线程会返回继续处理你的逻辑.<br>
你可能会想到  PIPE OR  Coroutine</p>
<p>Pipe modes: 他是双向打洞模式</p>
<div class="mermaid"><svg xmlns="http://www.w3.org/2000/svg" id="mermaid-svg-5ApVsPaomOL1G7IG" style="max-width:450px;" viewBox="-50 -10 450 231" width="100%" height="100%"><g /><g><line class="actor-line" id="actor247" stroke="#999" stroke-width="0.5px" x1="75" y1="5" x2="75" y2="220" /><rect class="actor" fill="#eaeaea" stroke="#666" x="0" y="0" width="150" height="65" rx="3" ry="3" /><text class="actor" style="text-anchor: middle;" alignment-baseline="central" dominant-baseline="central" x="75" y="32.5"><tspan x="75" dy="0">await read</tspan></text></g><g><line class="actor-line" id="actor248" stroke="#999" stroke-width="0.5px" x1="275" y1="5" x2="275" y2="220" /><rect class="actor" fill="#eaeaea" stroke="#666" x="200" y="0" width="150" height="65" rx="3" ry="3" /><text class="actor" style="text-anchor: middle;" alignment-baseline="central" dominant-baseline="central" x="275" y="32.5"><tspan x="275" dy="0">Sock.Recv</tspan></text></g><defs><marker id="arrowhead" refX="5" refY="2" markerWidth="6" markerHeight="4" orient="auto"><path d="M 0 0 V 4 L 6 2 Z" /></marker></defs><defs><marker id="crosshead" refX="16" refY="4" markerWidth="15" markerHeight="8" orient="auto"><path style="stroke-dasharray: 0px, 0px;" fill="black" stroke="#000000" stroke-width="1px" d="M 9 2 V 6 L 16 4 Z" /><path style="stroke-dasharray: 0px, 0px;" fill="none" stroke="#000000" stroke-width="1px" d="M 0 1 L 6 7 M 6 1 L 0 7" /></marker></defs><g><text class="messageText" style="text-anchor: middle;" x="175" y="93">读取打洞</text><line class="messageLine1" style="fill: none; stroke-dasharray: 3px, 3px;" marker-end="url(&quot;#arrowhead&quot;)" stroke="black" stroke-width="2" x1="75" y1="100" x2="275" y2="100" /></g><g><text class="messageText" style="text-anchor: middle;" x="175" y="128">返回打洞</text><line class="messageLine1" style="fill: none; stroke-dasharray: 3px, 3px;" marker-end="url(&quot;#arrowhead&quot;)" stroke="black" stroke-width="2" x1="275" y1="135" x2="75" y2="135" /></g><g><rect class="actor" fill="#eaeaea" stroke="#666" x="0" y="155" width="150" height="65" rx="3" ry="3" /><text class="actor" style="text-anchor: middle;" alignment-baseline="central" dominant-baseline="central" x="75" y="187.5"><tspan x="75" dy="0">await read</tspan></text></g><g><rect class="actor" fill="#eaeaea" stroke="#666" x="200" y="155" width="150" height="65" rx="3" ry="3" /><text class="actor" style="text-anchor: middle;" alignment-baseline="central" dominant-baseline="central" x="275" y="187.5"><tspan x="275" dy="0">Sock.Recv</tspan></text></g></svg></div>
<p>ZYV modes:是单项打洞模式,读取的时候是弹出栈到Socket层,Socket读取到数据后使用PIPE返回当前的内容上下文继续运行.内部采用异步Stream模式.方便支持各种流载体比如SSLStream,TlsStream,GzipStream…</p>
<div class="mermaid"><svg xmlns="http://www.w3.org/2000/svg" id="mermaid-svg-WkxWEOqkKpKh33HE" style="max-width:450px;" viewBox="-50 -10 450 231" width="100%" height="100%"><g /><g><line class="actor-line" id="actor249" stroke="#999" stroke-width="0.5px" x1="75" y1="5" x2="75" y2="220" /><rect class="actor" fill="#eaeaea" stroke="#666" x="0" y="0" width="150" height="65" rx="3" ry="3" /><text class="actor" style="text-anchor: middle;" alignment-baseline="central" dominant-baseline="central" x="75" y="32.5"><tspan x="75" dy="0">await read</tspan></text></g><g><line class="actor-line" id="actor250" stroke="#999" stroke-width="0.5px" x1="275" y1="5" x2="275" y2="220" /><rect class="actor" fill="#eaeaea" stroke="#666" x="200" y="0" width="150" height="65" rx="3" ry="3" /><text class="actor" style="text-anchor: middle;" alignment-baseline="central" dominant-baseline="central" x="275" y="32.5"><tspan x="275" dy="0">Sock.Recv</tspan></text></g><defs><marker id="arrowhead" refX="5" refY="2" markerWidth="6" markerHeight="4" orient="auto"><path d="M 0 0 V 4 L 6 2 Z" /></marker></defs><defs><marker id="crosshead" refX="16" refY="4" markerWidth="15" markerHeight="8" orient="auto"><path style="stroke-dasharray: 0px, 0px;" fill="black" stroke="#000000" stroke-width="1px" d="M 9 2 V 6 L 16 4 Z" /><path style="stroke-dasharray: 0px, 0px;" fill="none" stroke="#000000" stroke-width="1px" d="M 0 1 L 6 7 M 6 1 L 0 7" /></marker></defs><g><text class="messageText" style="text-anchor: middle;" x="175" y="93">pop stack</text><line class="messageLine0" style="fill: none;" marker-end="url(&quot;#arrowhead&quot;)" stroke="black" stroke-width="2" x1="75" y1="100" x2="275" y2="100" /></g><g><text class="messageText" style="text-anchor: middle;" x="175" y="128">返回打洞</text><line class="messageLine1" style="fill: none; stroke-dasharray: 3px, 3px;" marker-end="url(&quot;#arrowhead&quot;)" stroke="black" stroke-width="2" x1="275" y1="135" x2="75" y2="135" /></g><g><rect class="actor" fill="#eaeaea" stroke="#666" x="0" y="155" width="150" height="65" rx="3" ry="3" /><text class="actor" style="text-anchor: middle;" alignment-baseline="central" dominant-baseline="central" x="75" y="187.5"><tspan x="75" dy="0">await read</tspan></text></g><g><rect class="actor" fill="#eaeaea" stroke="#666" x="200" y="155" width="150" height="65" rx="3" ry="3" /><text class="actor" style="text-anchor: middle;" alignment-baseline="central" dominant-baseline="central" x="275" y="187.5"><tspan x="275" dy="0">Sock.Recv</tspan></text></g></svg></div>
<p>以前的ZYSOCKET框架采用了 A B模式,当Recv到数据包的时候,读取长度压入缓冲区.最后待数据包完整后,返回完整的数据包.注意这里有个 数据包缓冲区,所以你需要COPY 一次BYTE[] AND 重新Read 并且 New BYTE[]数据包.而且数据包缓冲区占用了一定的内存.这样的套路优点在于简单,稳定性好.缺点在于 性能无法发挥到极致.因为有GC,所以拖累了CPU,应为有缓冲区,所以拖累了内存.</p>
<p>当然也有做的好的,比如IKende的BX,采用数据链,绑定到SOCKET上. 我称他为A B C模式. 当RECV到数据先写入到数据链表(B)中,SOCKET(A) 始终在写你的下一个或者下下个数据链表项,让你©感觉你再读一条数据长链. 这样的好处在于,你可以减少一次COPY. 但是缺点也很明显你需要维护一个很长的数据链表.CPU不敢说,但是内存是绝对需要很大的量来维持数据链表的. 而且你还需要一个线程池来平衡A端和C端.当然如果用上SSL 协议.那么这套逻辑的唯一GET点在于可以方便的嵌套SSLStream.不过BX是一款非常优秀的SOCKET框架.除了内存高了点外,(这没办法设计模式).</p>
<p><strong>性能方面:</strong><br>
CPU 性能方面是非常高的.你不要看到它使用了await 就认为他性能不行了,为啥因为await生成了其他的代码.其实那点代码可以略微不计.总体性能可以保证不低于 或者 非常接近 C++ OR GOLANG 编写的主流SOCKET 框架.<br>
内存方面: 除了基本运行内存 (<a href="http://xn--4gqy23dt3sh4i.NET">一般来说.NET</a> CORE 程序大概是10M左右).连接数X你缓冲区的 内存大小了. 比如你每个链接4K.那么设置1W链接就是39MB. 你也许会问.为什么你要先创建.不会动态分配吗?那样更少,虽然延迟创建固然好,但是节省了内存,换来的是你在那刹那增加的CPU.和不稳定性.<br>
GC方面:<br>
因为使用了PIPE,所以缓冲区我们基本就没有了,但是 如果你需要直接ReadBytes() 返回一个BYTE[] 数组的时候怎么办呢?这个真没办法,如果你需要返回一个BYTE[],那只能NEW.但是你如果需要读取一个Memory or SPAN 我采用了 MemoryPool 的方式在内存池里给你分配一段大小适中的内存. 然后将数据COPY到这块内存中,供你使用. 这样就可以避免GC了. 当然用完记得释放它.而且你也可以将它转换成BYTE[]使用,记住INDEX LEN 简单的方法:</p>
<pre><code>   using (var p8 = await fiberRw.ReadMemory())
         {
                var mem = p8.Value.GetArray();
                byte[] data = mem.Array;
                int index = mem.Offset;
                int len = mem.Count;
                .......
         }
</code></pre>
<p><a href="http://xn--ykq9w171g.NET">使用了.NET</a> STD 2发布. 没办法为了支持所有的.NET支持的平台,<a href="http://xn--ykqt1xwveipaw49k.NET">舍弃了很多.NET</a> CORE的新功能.可是使用的平台包括 .NET FX; .NETCORE; MONO;. XAMARIM IOS;XARMARIM ANDROID. (注意如果是IOS系统请使用[Portable]版,因为IOS AOT禁用EMIT,<a href="http://xn--PROTOBUFF-927nx55bi25bdyk.NET">所以只是PROTOBUFF.NET</a> 使用了一个没有EMIT的版本.</p>
<p>支持stream嵌套操作,比如SSLStream GzipStream,详细可以看DEMO.</p>
<h2 id="关于如何使用">关于如何使用:</h2>
<p>我目前提供了一个DEMO的多种版本供参考<a href="https://github.com/luyikk/ZYSOCKET-V/tree/master/Demo">DEMO URL</a></p>
<h2 id="注意事项"><strong>注意事项:</strong></h2>
<p>本框架对于一个连接来说,在相同时间里,只有一个线程在处理它.不是在SOCKET RECV,就是在处理await Read. 这样设计有个好处在于,1 你不需要QUEUE,2 对于当前连接来说,它的所有资源你都无需LOCK, 但是也有坏处 所以如果逻辑上对于这个线程堵塞了.那么就会降低性能. 例如同步读取数据库.同步GET一个URL.等.<br>
那么如何处理呢. 当你读取到基本数据,处理完基本逻辑时. 需要处理复杂逻辑,你就需要  call async void method 什么意思呢.下面来个代码示范.</p>
<pre><code>        static async ValueTask DataOnByLine(IFiberRw&lt;string&gt; fiberRw)
        {
            var cmd = await fiberRw.ReadInt32();
            var p1 = await fiberRw.ReadInt32();
            var p2 = await fiberRw.ReadInt64();
            ReadData(cmd, p1, p2);
        }

        static async void ReadData(int? cmd,int? p1,long? p2)
        {
            try
            {
               //运行逻辑
                var data = await DataBase.Select("select * from table"); //调用数据库是非常久的.
              // 使用回调线程继续运行逻辑
            }
            catch(Exception er)
            {
                Log.Error(er);
            }
        }
</code></pre>
<p><strong>注意上面的 static async void ReadData,以及try catch,因为如果这里的异常 你根本无法外面捕捉,所以需要加一个try catch捕捉异常,否则异常的话将导致服务器崩溃.</strong></p>
<h2 id="有什么问题或者有什么工作的机会可以加我qq547386448">有什么问题,或者有什么工作的机会,可以加我QQ:547386448</h2>
<p>最后来个SHOW ECHO RPS的IMAGE<br>
<img alt="enter image description here" src="https://github.com/luyikk/ZYSOCKET-V/blob/master/Benchmarks/echoBenchmarks.png?raw=true"></p>
</div>
</body>

</html>
