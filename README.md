# 数据结构课程设计-SimpleFS 简易树形文件系统的文件资源管理器
### 请注意：本文件系统相对常见的文件系统而言性能较差，可能存在未知BUG，编写主要目的在于完成数据结构课程设计，不建议用于实际项目
## 编译教程
### 编译前的准备工作
1、安装Visual Studio 2022和.Net开发工具  
2、`git clone https://github.com/diylxy/MyFileBrowser.git`  
之后根据自己的喜好选择以下两种编译方法之一：  
### 使用CMD编译
3、打开VS2022开发人员命令提示符，切换到本仓库所在目录，执行`msbuild SimpleFS文件资源管理器.sln`  
### 使用GUI编译
3、使用VS2022打开解决方案，选择“生成-生成解决方案”  
## 使用方法
在bin目录下找到生成的exe文件，将其与编译[MyFileSystem](https://github.com/diylxy/MyFileSystem)时生成的`MyFileSystem\build\Debug\SimpleFS.dll`放在同一目录下，双击打开程序即可。  
