import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import { useRouter } from 'vue-router'

export const useAuthStore = defineStore('auth', () => {
    const token = ref(localStorage.getItem('token') || '')
    const user = ref(null)
    const router = useRouter()

    const isAuthenticated = computed(() => !!token.value)

    function setToken(newToken: string) {
        token.value = newToken
        localStorage.setItem('token', newToken)
    }

    function logout() {
        token.value = ''
        user.value = null
        localStorage.removeItem('token')
        router.push('/login')
    }

    return { token, user, isAuthenticated, setToken, logout }
})
