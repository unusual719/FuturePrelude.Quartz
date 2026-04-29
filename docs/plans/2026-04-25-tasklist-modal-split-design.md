# TaskListView.vue 弹框组件拆分设计

## 目标

将 `TaskListView.vue` 中的 4 个弹框组件拆分到独立的 `.vue` 文件，降低主文件复杂度至约 1000 行以内。

## 组件拆分

| 组件文件 | 职责 | 关键 Props | 关键 Emit |
|---------|------|-----------|----------|
| `GroupModal.vue` | 分组新增/编辑 | `visible`, `group` (null=新建) | `close`, `save` |
| `HttpJobModal.vue` | HTTP 任务新增/编辑 | `visible`, `job` (null=新建) | `close`, `save` |
| `AssemblyJobModal.vue` | 插件任务新增/编辑 | `visible`, `job` (null=新建) | `close`, `save` |
| `TriggerModal.vue` | 触发器新增/编辑 | `visible`, `trigger`, `jobId` | `close`, `save` |
| `RecordModal.vue` | 执行记录查看 | `visible`, `records`, `title` | `close` |

## Props/Emit 约定

每个弹框组件的标准接口：

```js
const props = defineProps({
  visible: Boolean,       // 弹框显示状态
  group: Object,          // GroupModal: null=新建, object=编辑
  job: Object,            // JobModal: null=新建, object=编辑
  trigger: Object,        // TriggerModal: null=新建, object=编辑
  records: Array,         // RecordModal: 记录列表
  title: String,          // RecordModal: 弹框标题
})

const emit = defineEmits(['close', 'save'])
```

## 文件结构

```
src/components/modals/
├── GroupModal.vue
├── HttpJobModal.vue
├── AssemblyJobModal.vue
├── TriggerModal.vue
└── RecordModal.vue
```

## TaskListView.vue 变更

- 移除模板中所有弹框 HTML（~250 行）
- 移除弹框相关的所有状态变量和逻辑方法（~340 行）
- 保留表格渲染、分组切换、tab 切换等核心逻辑
- 父组件统一管理 `visible` 状态

## 数据流示例（分组保存为例）

```
TaskListView.vue          GroupModal.vue
     │                        │
     │  :visible="groupModalVisible"  │
     │  :group="editingGroup"  │
     │ ──────────────────────>│
     │                        │
     │              [用户填写表单]
     │                        │
     │  @save="handleGroupSave"│
     │ <──────────────────────│
     │  payload = {Name, Icon} │
     │                        │
     │  [调用 API 保存]       │
     │                        │
     │  groupModalVisible=false │
```

## 实施步骤

1. 创建 `src/components/modals/` 目录
2. 创建 `GroupModal.vue`
3. 创建 `HttpJobModal.vue`
4. 创建 `AssemblyJobModal.vue`
5. 创建 `TriggerModal.vue`
6. 创建 `RecordModal.vue`
7. 重构 `TaskListView.vue`，移除弹框相关代码，引入子组件
