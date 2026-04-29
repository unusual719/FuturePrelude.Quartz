import { createRouter, createWebHistory } from 'vue-router'
import { getPostLoginRedirectPath } from "../utils/authSession.js";

// 路由配置
const routes = [
    {
        path: '/',
        redirect: '/login'
    },
    {
        path: '/login',
        name: 'Login',
        component: () => import('../views/LoginView.vue')
    },
    {
        path: '/dashboard',
        name: 'Dashboard',
        component: () => import('../views/DashboardView.vue'),
        meta: { requiresAuth: true }
    },
    {
        path: '/task-list',
        name: 'TaskList',
        component: () => import('../views/TaskListView.vue'),
        meta: { requiresAuth: true }
    },
    {
        path: '/records',
        name: 'ExecutionRecords',
        component: () => import('../views/ExecutionRecordsView.vue'),
        meta: { requiresAuth: true }
    }
]

// 创建路由实例
const router = createRouter({
    history: createWebHistory(),
    routes
})

// 路由守卫：检查登录状态
router.beforeEach((to, from, next) => {
    const token = localStorage.getItem('scheduler_token')
    const redirectPath = getPostLoginRedirectPath(to.fullPath)

    if (to.meta.requiresAuth && !token) {
        // 需要登录但没有 token，跳转到登录页
        next({ name: 'Login', query: { redirect: redirectPath } })
    } else if (to.name === 'Login' && token) {
        // 已登录访问登录页，跳转到任务列表
        next(getPostLoginRedirectPath(to.query?.redirect))
    } else {
        next()
    }
})

export default router
