import { createRouter, createWebHistory } from 'vue-router'
import HomeView from '../views/HomeView.vue'

const router = createRouter({
    history: createWebHistory(import.meta.env.BASE_URL),
    routes: [
        {
            path: '/',
            name: 'home',
            component: HomeView
        },
        {
            path: '/login',
            name: 'login',
            component: () => import('../views/LoginView.vue')
        },
        {
            path: '/signup',
            name: 'signup',
            component: () => import('../views/SignupView.vue')
        },
        {
            path: '/document',
            name: 'document',
            component: () => import('../views/DocumentView.vue')
        },
        {
            path: '/keys',
            name: 'keys',
            component: () => import('../views/KeyManagementView.vue'),
            meta: { requiresAuth: true }
        },
        {
            path: '/term',
            name: 'term',
            component: () => import('../views/TermsView.vue')
        },
        {
            path: '/privacy',
            name: 'privacy',
            component: () => import('../views/PrivacyView.vue')
        }
    ]
})

router.beforeEach((to, _from, next) => {
    const token = localStorage.getItem('token')
    if (to.meta.requiresAuth && !token) {
        next('/login')
    } else {
        next()
    }
})

export default router
