# M

简介
----
这是一款使用 Unity 开发的体感音乐节奏游戏，通过 Leap Motion 进行控制。    

**游戏演示：http://www.bilibili.com/video/av11165700/**    

由于 Leap Motion Orion SDK 的限制，目前仅支持 Windows。    
Unity 版本：5.5.1f1。    
运行 Scenes/Init.unity 以开始游戏。    
**注意：项目中不包含任何歌曲，谱面，音效资源，需要手动添加。**    
<br/>
<br/>

目录结构
----
- Assets  
  - 3rdLibrary：存放项目用到的第三方库
  - Plugins：存放项目用到的插件
  - Res：存放项目资源，包括特效，材质，Prefab，天空盒等
  - Resources：内置文件目录，主要用于存放内置歌曲
  - Scenes：游戏场景目录
  - Scripts：游戏脚本目录
  - Streaming Assets：自定义歌曲存放目录
- Beatmaps：谱面转换工具工作目录
<br/>

转换 osu! 谱面
----
在 Client/Beatmaps 目录中建立文件夹，每个文件夹代表一首音乐。    
在每个音乐文件夹下，按照字典序，放置 1-3 个对应音乐的 osu! 谱面。    
在 Unity Editor 中选择 Tools/Convert Beatmap，就可以把 osu! 谱面转换为项目中可以直接使用的 JSON 格式，输出到 Client/Beatmaps 目录下，文件名和目录名相同。    
<br/>
放置示例：    

- Beatmaps
  - Music1
    - beatmap1.osu
    - beatmap2.osu
    - beatmap3.osu
  - Music2
    - beatmap.osu
  - Music3
    - beatmap1.osu
    - beatmap2.osu
    
生成结果示例：    

- Beatmaps
  - Music1.json
  - Music2.json
  - Music3.json
  
三个谱面文件会依次对应本游戏中的 Easy/Normal/Hard 难度。
<br/>
<br/>

添加内置歌曲
----
内置歌曲全部存放在 Resources/Music 目录下。    
谱面文件全部放置在 Resources/Music/Beatmaps 目录下，文件命名没有要求。    
音乐文件全部放置在 Resources/Music/Audio 目录下，文件命名要和谱面文件中的一致。    
专辑图片文件全部放置在 Resources/Music/Banner 目录下，文件命名要和谱面文件中的一致。    
音效文件全部放置在 Resources/Music/SoundEffect 目录下，文件命名要和谱面文件中的一致。    
<br/>
<br/>

添加自定义歌曲
----
直接把 MP3 文件放入 StreamingAssets 目录即可。    
打包之后，也可以通过把 MP3 文件放入游戏的 M_Data/StreamingAssets 目录，来在运行时添加歌曲。    
