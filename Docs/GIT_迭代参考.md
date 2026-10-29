# Git 迭代参考

## 目标

一次可验证的小步改动，对应一次提交。不要把多个需求、自动生成文件和临时试验混在同一个提交里。

## 分支与基线

- `main`：可继续开发的主线。
- `baseline/2026-10-29-pre-iteration`：开始迭代前的原始工程快照，只用于回看与比较。
- 每项功能从 `main` 新建短期分支，例如 `iter-001-asset-structure`、`iter-002-player-control`。

## 一次步进的固定流程

1. 从表格选择一个最小验收项，并写清验收方式。
2. 只实现该项所需的资源、Prefab、脚本或场景改动。
3. 在 Unity 中验证后检查 `git status`，确认没有 `Library`、`Temp`、日志等生成文件。
4. 提交并写明编号、范围与可验证结果。
5. 更新需求表中的状态和完成比例，再单独提交该表格更新（如果表格也放入仓库）。

## 提交信息格式

```text
iter-001 assets: split dock boat from map source
iter-002 player: add third-person movement and jump
iter-003 scene: add dock collision boundary
iter-004 gem: activate first gem and restore dock color
docs: update requirement progress for iter-004
```

格式为：`<迭代编号或类型> <范围>: <本次已验收的事实>`。不要使用“修改一下”“测试”“临时”等无法追溯的描述。

## Unity 资产规则

- 资源文件与同名 `.meta` 必须一起提交；不要手动删除或重建 `.meta`。
- `Library`、`Temp`、`Logs`、`UserSettings` 一律不提交。
- FBX、音视频、PSD、Blend 等二进制源资产由 Git LFS 管理。
- 场景中只保存具名逻辑根节点与配置；美术模型通过可替换的子节点或 Prefab 引用接入。

## 提交前检查

```powershell
git status --short
git diff --check
git lfs ls-files
```

首要原则：一条提交应当能回答“改了什么、如何验证、必要时如何回退”。
