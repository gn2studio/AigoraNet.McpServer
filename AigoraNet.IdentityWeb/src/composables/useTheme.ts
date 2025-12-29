import { ref, watch, onMounted } from 'vue'

const isDark = ref(false)
const isInitialized = ref(false)

export function useTheme() {
    const updateDOM = (dark: boolean) => {
        if (dark) {
            document.documentElement.classList.add('dark')
            document.documentElement.classList.remove('light')
            localStorage.setItem('theme', 'dark')
        } else {
            document.documentElement.classList.add('light')
            document.documentElement.classList.remove('dark')
            localStorage.setItem('theme', 'light')
        }
    }

    // Watch for changes in isDark and update DOM automatically
    watch(isDark, (newVal) => {
        updateDOM(newVal)
    })

    const toggleTheme = () => {
        console.log('Toggling theme. Current:', isDark.value)
        isDark.value = !isDark.value
    }

    const initTheme = () => {
        if (isInitialized.value) return

        const savedTheme = localStorage.getItem('theme')

        // Default to dark unless explicitly set to light
        if (savedTheme === 'light') {
            isDark.value = false
        } else {
            isDark.value = true
        }

        // Ensure DOM is in sync with initial state
        updateDOM(isDark.value)
        isInitialized.value = true
    }

    onMounted(() => {
        initTheme()
    })

    return { isDark, toggleTheme }
}
