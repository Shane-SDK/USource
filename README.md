>[!CAUTION]
>### This is in a very rough state right now and is not build/production ready yet
>- References to the UnityEditor namespace are everywhere and are not guarded by preprocessor conditions. This means you will not be able to successfully build your project.
>- Asset browser does not show preview images for content
>- Has only been tested on content from 2013 Source Engine games, specifically Garry's Mod, Half Life 2, and Counter Strike: Source files.
>
>Despite the current state I want to publicize this for anyone who may be interested in its development. I plan on using this in my other projects so it is important to me that these problems get solved in the future

USource is a plugin that lets you import Source Engine content such as materials, textures, models, and maps.

Specifically it uses Unity's AssetImporter classes to allow Source's proprietary file formats to be converted into their Unity-counterparts. This means any assets converted are handled in a way consistent with how the Unity Editor expects you to use assets. 

In addition an Asset Browser is provided that allows users to browse and import any Source assets found in their games' folders into their project.

>[!NOTE]
>USource is a heavily modified version of [uSource by DeadZoneLuna.](https://github.com/DeadZoneLuna/uSource)

## Source Asset Support Status
|Source File Type|Asset Type|Status||
|-|-|-|-|
|mdl|Model|Prefabs/GameObjects|✔️|
|vtf|Texture|Texture2D|✔️|
|vmt|Material|Material|✔️|
|vmf|Uncompiled Level|Prefab/RealTimeCSG Brushes|✔️|
|bsp|Compiled Level|In the future|:x:|
|pcf|Particles|In the future|:x:|

>[!NOTE]
>VMF files use [RealtimeCSG](https://realtimecsg.com/) brushes for the level geometry. It is provided in this repo.

## MDL Data Support Status
|MDL Data|Status||
|-|-|-|
|Mesh|Supported|✔️|
|Bodygroups|In the future|:x:|
|Shape keys|In the future|:x:|
|Hitboxes|In the future|:x:|
|Animations|In the future|:x:|
|Physics/Colliders|Supported|✔️|
