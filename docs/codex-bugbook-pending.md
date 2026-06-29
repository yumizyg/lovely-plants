## 2026-06-29 - 底部计时条与日常提示缺失

- Status: pending
- Trigger: 用户反馈希望底部显示工作时间和番茄钟，并指出“并没看到任何的日常提示语”
- Symptom: 花盆栏下方没有会话计时/番茄钟入口；15 分钟随机提示未按预期出现
- Context: `src/DesktopGarden/GardenForm.cs`, `src/DesktopGarden/GardenRenderer.cs`, `src/DesktopGarden/GardenApplicationContext.cs`
- Initial read: 现有会话秒表与成长累计秒表职责混杂风险较高；提示语触发与窗口显示链路分离，缺少可见性保障
- Next check: 将会话计时与提示调度收拢到花园窗口心跳，补充底部交互区并实机验证提醒弹出
