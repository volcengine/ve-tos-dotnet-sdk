# 火山引擎对象存储服务 .Net SDK

## 版本
- 当前版本：2.1.0

## 运行环境

### Windows
- 适用于 `.NET Framework 2.0` 及以上版本
- 适用于 `.NET Core 2.0` 及以上版本
- 适用于 `Visual Studio 2019`及以上版本

### Linux/Mac
- 适用于 `Mono 6.12` 及以上版本

## 安装方法
### Windows 环境安装
#### NuGet 安装
- 如果您的 Visual Studio 没有安装 NuGet，请先安装 [NuGet](http://docs.nuget.org/docs/start-here/installing-nuget).
- 安装好NuGet后，先在 `Visual Studio` 中新建或者打开已有的项目，然后选择`<工具>`－`<NuGet程序包管理器>`－`<管理解决方案的NuGet程序包>`，
- 搜索 `Volcengine.TOS.SDK`，在结果中找到 `Volcengine.TOS.SDK`，选择最新版本，点击安装，成功后添加到项目应用中。

#### DLL 引用方式安装
- 在 Github 下载最新版本的 TOS .Net SDK 项目代码。
- TOS .Net SDK 代码库中 .Net Framework 和 .Net Core 版本共享一份代码，按需选择需要编译的项目。
  - .Net Framework 版本编译 TOS/TOS.csproj 对应的项目
  - .Net Core 版本编译 TOSNetCore/TOSNetCore.csproj 对应的项目
- 选择 Release 模型，编译 ve-tos-dotnet-sdk 项目，生成 DLL。
- 在 Visual Studio 的`<解决方案资源管理器>`中选择您的项目，然后右键`<项目名称>`-`<引用>`，在弹出的菜单中选择`<添加引用>`，
  在弹出`<添加引用>`对话框后，选择`<浏览>`，找到SDK编译产物的目录，在 bin 目录下选中 `<Volcengine.TOS.dll>` 文件,点击确定即可

#### 项目引入方式安装
- 在 Github 下载最新版本的 TOS .Net SDK 项目代码。
- 在 VS 中打开或新建项目，右键单击解决方案，在弹出的在弹出的菜单中单击添加现有项目。
- TOS .Net SDK 代码库中 .Net Framework 和 .Net Core 版本共享一份代码，按需选择项目。
  - .Net Framework 版本，在弹出框中选择 TOS.csproj 文件，单击打开
  - .Net Core 版本，在弹出框中选择 TOSNetCore.csproj 文件，单击打开
- 接下来右键`<您的项目>`－`<引用>`，选择`<添加引用>`，在弹出的对话框选择`<项目>`选项卡后选中上一步中您选中的项目，点击确定即可。

### Linux/Mac 环境安装
#### NuGet 安装
- 先在 `Xamarin` 中新建或者打开已有的项目，然后选择`<工具>`－`<Add NuGet Packages>`。
- 搜索 `Volcengine.TOS.SDK`，在结果中找到 `Volcengine.TOS.SDK`，选择最新版本，点击 `<Add Package>`，成功后添加到项目应用中。

## 快速使用
#### 使用 TOS Endpoint 创建Client（Create TOS Client）
```csharp
    using TOS;
    
    namespace ConsoleApp
    {
        internal class Program
        {
            private static void Main(string[] args)
            {
                var ak = "*** Provide your access key ***";
                var sk = "*** Provide your secret key ***";
                // endpoint 若没有指定HTTP协议（HTTP/HTTPS），默认使用 HTTPS
                var endpoint = "*** Provide your endpoint ***";
                var region = "*** Provide your region ***";
    
                // 创建TOSClient实例。
                var client = TosClientBuilder.Builder().SetAk(ak).SetSk(sk).SetEndpoint(endpoint).SetRegion(region).Build();
            }
        }
    }
```

#### 创建存储空间（Create Bucket）
```csharp
    using System;
    using TOS;
    using TOS.Error;
    using TOS.Model;
    
    namespace ConsoleApp
    {
        internal class Program
        {
            private static void Main(string[] args)
            {
                var ak = "*** Provide your access key ***";
                var sk = "*** Provide your secret key ***";
                // endpoint 若没有指定HTTP协议（HTTP/HTTPS），默认使用 HTTPS
                var endpoint = "*** Provide your endpoint ***";
                var region = "*** Provide your region ***";
                // 填写 BucketName
                var bucketName = "*** Provide your bucket name ***";
    
                // 创建TOSClient实例
                var client = TosClientBuilder.Builder().SetAk(ak).SetSk(sk).SetEndpoint(endpoint).SetRegion(region).Build();
    
                try
                {
                    // 创建存储空间输入
                    var createBucketInput = new CreateBucketInput()
                    {
                        Bucket = bucketName
                    };
                    // 创建存储空间。
                    var createBucketOutput = client.CreateBucket(createBucketInput);
                    Console.WriteLine("Create bucket succeeded, request id {0} ", createBucketOutput.RequestID);
                    Console.WriteLine("Create bucket succeeded, status code {0} ", createBucketOutput.StatusCode);
                }
                catch (TosServerException ex)
                {
                    Console.WriteLine("Create bucket failed, request id {0}", ex.RequestID);
                    Console.WriteLine("Create bucket failed, status code {0}", ex.StatusCode);
                    Console.WriteLine("Create bucket failed, response error code {0}", ex.Code);
                    Console.WriteLine("Create bucket failed, response error message {0}", ex.Message);
                }
                catch (TosClientException ex)
                {
                    Console.WriteLine("Create bucket failed, error message {0}", ex.Message);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Create bucket failed, {0}", ex.Message);
                }
            }
        }
    }
```

#### 上传文件（Put Object）
```csharp
    using System;
    using System.IO;
    using System.Text;
    using TOS;
    using TOS.Error;
    using TOS.Model;
    
    namespace ConsoleApp
    {
        internal class Program
        {
            private static void Main(string[] args)
            {
                var ak = "*** Provide your access key ***";
                var sk = "*** Provide your secret key ***";
                // endpoint 若没有指定HTTP协议（HTTP/HTTPS），默认使用 HTTPS
                // Bucket 的 Endpoint，以华北2（北京）为例：https://tos-cn-beijing.volces.com
                var endpoint = "https://tos-cn-beijing.volces.com";
                var region = "cn-beijing";
                // 填写 BucketName
                var bucketName = "*** Provide your bucket name ***";
                // 填写对象名
                var objectKey = "*** Provide your object key ***";
                // 上传对象 Body ， 以 string 对象为例
                var objectContent = "object content";
                // 创建TOSClient实例
                var client = TosClientBuilder.Builder().SetAk(ak).SetSk(sk).SetEndpoint(endpoint).SetRegion(region).Build();
    
                try
                {
                    var binaryData = Encoding.UTF8.GetBytes(objectContent);
                    using (var requestContent = new MemoryStream(binaryData))
                    {
                        // 创建上传文件输入
                        var putObjectInput = new PutObjectInput
                        {
                            Bucket = bucketName,
                            Key = objectKey,
                            Content = requestContent
                        };
                        // 上传文件
                        var putObjectOutput = client.PutObject(putObjectInput);
                        Console.WriteLine("Put object succeeded, ETag: {0} ", putObjectOutput.ETag);
                    }
                }
                catch (TosServerException ex)
                {
                    Console.WriteLine("Put object failed, request id {0}", ex.RequestID);
                    Console.WriteLine("Put object failed, status code {0}", ex.StatusCode);
                    Console.WriteLine("Put object failed, response error code {0}", ex.Code);
                    Console.WriteLine("Put object failed, response error message {0}", ex.Message);
                }
                catch (TosClientException ex)
                {
                    Console.WriteLine("Put object failed, error message {0}", ex.Message);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Put object failed, {0}", ex.Message);
                }
            }
        }
    }
```

#### 其它
- 上面的例子中，如果没有抛出异常则说明执行成功，否则失败。更多例子请查看[官网文档](https://www.volcengine.com/docs/6349/93480)。
