# 星旅物语 · C# 极简主菜单原型

本工程已将菜单逻辑改写为 **Godot .NET / C#**，没有 GDScript 运行时依赖。背景是独立的无字插画，标题与菜单是实际 Godot 控件，并非截图上的按钮热区。游戏名为暂定名。

**验证状态：本 C# 版本尚未编译或运行验证。** 制作环境没有 .NET SDK，因此本包是待构建的源代码工程，不是已验证的 Windows 可执行程序。旧 GDScript 版本的测试结果不代表 C# 版本通过测试。

## 环境与运行

- Godot **4.7.2 .NET 版**；标准版无法运行 C# 脚本。
- **.NET 8 SDK**（不是仅 Runtime）；项目目标为 `net8.0`，使用 `Godot.NET.Sdk/4.7.2`。
- 首次还原需要对应 NuGet 包可用。若环境缺少组件，请先完成获准的安装，再构建。

Windows 建议解压到 `D:\work\star-traveler-csharp`。本说明中的路径只是建议位置，不表示已复制到该电脑。

1. 在 Godot .NET 项目管理器导入 `project.godot`。
2. 等待资源导入，点击「构建」；成功后按 F5 运行。
3. 或打开 `StarTraveler.sln` 使用支持 .NET 的编辑器。

PowerShell 示例（将 `godot` 替换为你电脑上的 Godot **.NET** 可执行文件或完整路径）：

```powershell
Set-Location 'D:\work\star-traveler-csharp'
dotnet --list-sdks
dotnet restore .\StarTraveler.csproj
dotnet build .\StarTraveler.csproj -c Debug
godot --editor --path . --import --quit
godot --path .
```

若使用带空格的 Godot 完整路径，请用 PowerShell 调用运算符，例如 `& 'C:\Tools\Godot .NET\Godot.exe' --path .`。工程内资源引用均为 `res://` 相对路径，可移动目录。

## 操作与范围

- 鼠标悬停、方向键 / Tab 选择，Enter / Space 确认；选中项只有细小横线。
- 「继续旅程」「载入存档」禁用，并提示暂无存档。本原型没有存档系统。
- 「新的冒险」打开明确标注的**非可玩关卡占位页**，可返回；没有角色移动、战斗或真实关卡。
- 「设置」可切换全屏 / 窗口、启动时的轻柔淡入，自动保存至 `user://settings.cfg`。
- 无循环动画、闪烁、视差或背景音效。
- Esc 在子页面返回，在主菜单打开退出确认；窗口关闭请求同样先询问。
- 保留原布局参数、中文衬线标题和疏朗间距；背景等比铺满，窄屏裁边而不拉伸。

## 文件

```text
StarTraveler.csproj         Godot C# SDK、net8.0、程序集设置
StarTraveler.sln            Visual Studio 解决方案
project.godot              入口、窗口、渲染器与 C# 特性
scenes/main.tscn            主场景，引用 MainMenu.cs
scripts/MainMenu.cs         菜单、布局、设置和退出确认
scripts/MenuButton.cs       鼠标 / 键盘焦点横线
assets/background.png       无字背景插画
assets/JourneySerifSC-Subset.otf  中文字体子集
assets/FONT-LICENSE.txt      字体版权与 SIL OFL 1.1
tests/SmokeTest.cs          C# 交互冒烟测试（尚未执行）
tests/smoke.tscn            独立测试场景
```

改文字 / 间距 / 字号可编辑 `MainMenu.cs` 的 `BuildMenu`、`Layout`、`BuildTheme`。接入游戏场景时替换 `prototype` 分支；未实现存档前不要启用继续 / 载入按钮。

## C# 测试（待有 SDK 环境后执行）

先成功构建、导入，再运行独立测试场景；不要使用旧版 GDScript 的 `--script` 命令：

```powershell
dotnet build .\StarTraveler.csproj -c Debug
godot --headless --editor --path . --import --quit
godot --headless --path . res://tests/smoke.tscn -- --test
```

测试覆盖菜单数量、禁用项、初始焦点、重复八次设置开关、返回焦点、设置持久化、全屏选项、Esc、占位页、窗口关闭确认、退出取消与确认。有图形环境去掉 `--headless`，额外检查真实显示模式和 4:3 菜单边界。测试使用独立 `user://settings.smoke.cfg` 并清理，不修改普通设置；`--test` 抑制确认退出时结束进程和启动淡入。

当前仅完成源代码/资源引用与包结构的静态检查；C# 编译、Godot .NET 导入、自动测试、Windows 显示与交互均待验证。包中不附伪造的 C# 运行截图或测试通过记录。

## 素材与许可

背景为本项目生成的独立插画。字体来自 Noto Serif CJK SC Regular，精简后命名为 JourneySerifSC-Subset.otf；原始版权及 SIL Open Font License 1.1 随包附带。新增中文文案可能超出子集范围，需重新生成字体子集或换用完整且授权合规的字体。

本包不包含 `.godot`、`bin`、`obj`、运行日志或生成的二进制文件。原 GDScript 工程保持独立，不会被本源码目录覆盖。
