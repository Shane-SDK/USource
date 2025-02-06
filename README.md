>[!WARNING]
>### This is in a very rough state right now and is not production ready yet
>- Has not been tested in builds (Does successfully build however)
>- Asset browser does not show preview images for content
>- Has only been tested on content from 2013 Source Engine games, specifically Garry's Mod, Half Life 2, and Counter Strike: Source files.
>- Centered around the Universal Rendering Pipeline
>- Inconsistent MDL importing relating to checksum errors
>- Some VPKs (Garry's Mod) are not read properly
>- Assets imported using the Asset Browser may need to be reimported through the Unity Project window in order for them to import correctly.  
>
>Despite the current state I want to publicize this for anyone who may be interested in its development. I plan on using this in my other projects so it is important to me that these problems get solved in the future

### USource is a plugin that lets you import Source Engine content such as materials, textures, models, and maps.
![usource-0](https://github.com/user-attachments/assets/cc022212-b615-4992-a03c-e6da600fa4de)

Specifically it uses Unity's AssetImporter classes to allow Source's proprietary file formats to be converted into their Unity-counterparts. This means any assets converted are handled in a way consistent with how the Unity Editor expects you to use assets. 

In addition an Asset Browser is provided that allows users to browse and import any Source assets found in their games' folders into their project.


>[!NOTE]
>USource is a heavily modified version of [uSource by DeadZoneLuna.](https://github.com/DeadZoneLuna/uSource)
>
>Any Source Engine files in your project require their dependencies (IE other files like vmts, vtfs...) to be present first in order for them to import correctly. The Asset Browser handles importing dependencies for you and should be your primary means of getting content in your project, as opposed to copy/pasting with your clipboard.

## Source Asset Support Status
|Source File Type|Asset Type|Status|
|-|-|-|
|mdl|Model|Prefabs/GameObjects|
|vtf|Texture|Texture2D|
|vmt|Material|Material|
|vmf|Uncompiled Level|Prefab/RealTimeCSG Brushes. <sup>[1]</sup> Buggy|
|bsp|Compiled Level|Prefabs/GameObjects|
|pcf|Particles|In the future|

>[!IMPORTANT]
>- [1] VMF files use [RealtimeCSG](https://realtimecsg.com/) brushes for the level geometry. It is not a requirement to use this plugin
>
>VMFs are only functional/supported if the RealtimeCSG symbol is present in your Project Settings. This symbol should appear automatically if RealtimeCSG is in your project.

## MDL Data Support Status
|Feature|Status|
|-|-|
|Mesh|Supported <sup>[1]</sup>|
|Bodygroups|In the future|
|Skins|In the future|
|Shape keys|In the future|
|Hitboxes|In the future|
|Animations|In the future|
|Physics/Colliders|Supported|
|Lightmaps|Supported|

>[!NOTE]
>- [1] Currently only the 'default' bodygroup/skin configuration is imported.

## BSP Data Support Status
|Feature|Status|
|-|-|
|World collision|Supported|
|World Geometry|Supported <sup>[1]</sup>|
|Displacements|Buggy <sup>[2]</sup>|
|Props|Supported|
|Brush entities|In the future|
|Skybox|In the future <sup>[3]</sup>|
|Light probes|Supported|
|Lightmaps|Supported <sup>[4]</sup>|
|Water|In the future|

>[!NOTE]
>- [1] BSP files have import options to change how world geometry is imported. You can import it as 1 mesh or have it split by visleafs. The latter is necessary for Unity's occlusion system as it allows sections of the world to be hidden.
>- [2] Displacements generally work but can appear flattened. Blended materials are not supported right now.
>- [3] You can choose to occlude the skybox from your BSP's mesh. However models that appear in the sky will not be occluded right now. Additionally Unity Sky Materials are not created at the moment.
>- [4] Lightmap UVs are generated by Unity's lightmap UV generator. It does not use the BSP's UVs.

## Images
![usource-3](https://github.com/user-attachments/assets/cb28e4f3-fc0a-4d65-a300-def4af2ae36a)

![usource-1](https://github.com/user-attachments/assets/2d0117d5-7019-4747-b6ed-2aed1f2a7844)

![settings](https://github.com/user-attachments/assets/176fc16e-2bbb-4ab7-ad97-5dfd5c576157)

![usource-4](https://github.com/user-attachments/assets/9d37d368-abac-4605-bb85-280d88dff75f)

