# ERFramework
一个游戏框架，目标是更新到模块化增加功能和加入xlua改UI为热更新

资源加载框架参考:https://github.com/fenghaiyang1121/RFramework


## 当前已完成功能：
### ver1.0:

定义ab包打包规则，ab包配置文件

Editor功能：ab包打包

类对象池

资源加载统一使用ab包

资源加载框架：资源和Gameobject

GameOjbect对象池

资源的默认配置，回收对象池内是可以还原

UI管理器

UI逻辑层基类

UI分离逻辑层和显示层

场景加载逻辑管理器配合loadingUI

配置表读取框架，使用txt文本，形式类似于Excel配置


### ver1.1:
Editor功能拓展：打开本地/缓存/ab包输出文件夹。一键将ab包覆盖至本地文件夹内

下载任务管理器

添加热更新检测下载资源的UI(CheckDownLoadingPanel)

ab包系统管理器：对比版本文件，下载需要更新的资源

CheckDownUI的逻辑功能

CheckDownPanel的显示功能

可添加：

版本文件加密\解密、ab包加密\解密

读取完版本文件后，可以判断下缓存区内文件是不是都存在。



## 未完成:

### Ver1.2:

加入客户端和服务端通信功能，完成loading到主城的部分


### Ver1.3:

模块化拓展背包，商店等功能


### Ver1.4:

加入xlua热更新,UI部分改为热更新
