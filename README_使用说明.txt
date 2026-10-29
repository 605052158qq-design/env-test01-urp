ENV_test01 | Unity 2022.3.62f1 + URP 14.0.11 | 修正版

完成的文件（不会把FBX原文件里的材质当成场景用材质）：
  Assets/Settings/ENV_URP.asset                 URP Pipeline Asset
  Assets/Settings/ENV_ForwardRenderer.asset     Universal Renderer / Forward
  ProjectSettings/GraphicsSettings.asset       默认启用 ENV_URP
  Assets/Scenes/ENV_test01_Main.unity           场景文件（Unity导入完成后自动组装并保存）
  Assets/Map/Models/ENV_test01_1021.FBX         原始FBX，不破坏模型
  Assets/Map/Materials/*.mat                    127个独立材质，保留FBX颜色或绑定贴图
  Assets/Map/Textures/*.png                     6张原始地图贴图

注意：这里没有Unity Editor，无法在本环境中实际完成Unity导入、运行或截图验证。
首次打开 Unity 2022.3.62f1：
  1. 在Unity Hub里添加这份工程的根目录；等待 Package Manager 和导入完成。
  2. 它会自动运行构建流程：设置URP、修复材质引用、将FBX实例化到场景、保存场景。
  3. 在 Project 面板点 Assets > Scenes > ENV_test01_Main （双击打开）。
  4. 如果还是空场景或粉色，运行菜单 Tools > ENV Test01 > 01 - Build or Repair Scene + URP + Materials。
  5. 运行菜单 Tools > ENV Test01 > 03 - Audit Pink & Embedded Materials，Console确认 embedded FBX slots=0；wrongShader=0；active pipeline=ENV_URP。
  6. 如果菜单不出现，先查看Console是否有Unity编译报错（Unity导入未完成也会如此）。

不要把本包Assets覆盖进已有工程中的同名材质目录之外（会影响GUID）;
推荐新建一个Unity Hub项目路径导入整包；如果你要在已有版本升级，备份ProjectSettings、Assets/Scenes及Assets/Map再替换。

保证范围：包内静态文件结构、资源对应关系、Unity Editor自动搭建和检查代码已静态检查；实际Unity加载仍要本地验证。
