# 桌面花园

一个轻量的 Windows 桌面植物应用。植物沿任务栏上方排列，可拖动换位、浇水、抚摸，并在累计运行 8 小时和 40 小时时成长到下一阶段。

## 使用

- 鼠标悬浮花盆：查看植物名称、成长阶段、累计运行时间和阶段进度。
- 按住左键并水平拖动：实时调整花盆顺序，邻近花盆会自动让位。
- 按住右键并拖动：移动整条花盆栏，位置会自动保存。
- 右键单击花盆：打开植物检查器，可浇水、抚摸、更换植物、花盆、表情或调整单盆缩放。
- 更换植物会将该花盆的成长时间归零，花盆、表情和缩放不会重置成长。
- 双击托盘图标或按 `Ctrl+Alt+G`：显示或隐藏花园。
- 托盘右键菜单：添加花盆、锁定鼠标穿透、打开设置或退出。
- 设置中可选择显示器、整体大小、始终置顶、声音和开机启动。

成长时间仅在软件运行期间累计。关闭软件不会产生离线成长，也不会出现植物死亡或缺水惩罚。

## 数据位置

用户状态保存在 `%LocalAppData%\DesktopGarden\state.json`。程序会原子写入该文件；无法解析的数据会自动改名为带 `corrupt-时间` 后缀的备份。

## 开发与构建

需要 .NET 8 SDK 与 Inno Setup 6：

```powershell
$dotnet = "$env:USERPROFILE\.dotnet\dotnet.exe"
& $dotnet test DesktopGarden.sln -c Release
& $dotnet publish src\DesktopGarden\DesktopGarden.csproj -c Release -r win-x64 --self-contained true -o artifacts\publish
& "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe" installer\DesktopGarden.iss
```

发布结果位于 `artifacts\publish`，安装包位于 `artifacts\DesktopGarden-Setup-1.1.0.exe`。

## 项目结构

- `src/DesktopGarden.Core`：成长、布局、状态模型和 JSON 持久化。
- `src/DesktopGarden`：透明桌面窗口、绘制、托盘、工具栏、选择器和设置。
- `tests/DesktopGarden.Tests`：阶段边界、持久化恢复和布局测试。
- `installer/DesktopGarden.iss`：标准 Windows 安装包定义。
