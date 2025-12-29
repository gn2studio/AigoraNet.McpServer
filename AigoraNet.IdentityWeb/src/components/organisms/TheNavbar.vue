<script setup lang="ts">
import { useAuthStore } from '@/stores/auth'
import { useTheme } from '@/composables/useTheme'
import { useI18n } from 'vue-i18n'
import { RouterLink } from 'vue-router'

const authStore = useAuthStore()
const { t, locale } = useI18n()
const { isDark, toggleTheme } = useTheme()

const changeLanguage = (event: Event) => {
  const target = event.target as HTMLSelectElement
  locale.value = target.value
  localStorage.setItem('locale', target.value)
}
</script>

<template>
  <nav class="app-navbar fixed w-full z-50 top-0 start-0 transition-all duration-300">
    <div class="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
      <div class="flex justify-between h-16 items-center">
        <!-- Logo -->
        <div class="flex-shrink-0 flex items-center">
          <RouterLink to="/" class="flex items-center gap-2">
            <div class="w-8 h-8 bg-gradient-to-br from-blue-500 to-purple-600 rounded-lg flex items-center justify-center text-white font-bold text-xl">A</div>
            <span class="text-2xl font-bold bg-clip-text text-transparent bg-gradient-to-r from-blue-600 to-purple-600 dark:from-blue-400 dark:to-purple-400">AIGORA</span>
          </RouterLink>
        </div>

        <!-- Desktop Menu -->
        <div class="hidden md:flex space-x-8 items-center">
          <RouterLink to="/" class="text-gray-600 dark:text-gray-300 hover:text-blue-600 dark:hover:text-blue-400 font-medium transition-colors">
            {{ t('home') }}
          </RouterLink>
          <RouterLink to="/document" class="text-gray-600 dark:text-gray-300 hover:text-blue-600 dark:hover:text-blue-400 font-medium transition-colors">
            {{ t('document') }}
          </RouterLink>
          <RouterLink v-if="authStore.isAuthenticated" to="/keys" class="text-gray-600 dark:text-gray-300 hover:text-blue-600 dark:hover:text-blue-400 font-medium transition-colors">
            {{ t('key_management') }}
          </RouterLink>
        </div>

        <!-- Right Side Actions -->
        <div class="flex items-center gap-4">
          <!-- Theme Toggle -->
          <button @click="toggleTheme" class="p-2 rounded-full text-gray-500 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-800 transition-colors focus:outline-none">
            <i :class="isDark ? 'fas fa-sun' : 'fas fa-moon'" class="text-lg"></i>
          </button>

          <!-- Language Selector -->
          <select v-model="locale" @change="changeLanguage" class="bg-transparent text-sm font-medium text-gray-600 dark:text-gray-300 border-none focus:ring-0 cursor-pointer">
            <option value="en">EN</option>
            <option value="ko">KO</option>
            <option value="ja">JA</option>
            <option value="zh">ZH</option>
          </select>

          <!-- Auth Buttons -->
          <div v-if="!authStore.isAuthenticated" class="flex items-center gap-3">
            <RouterLink to="/login" class="text-gray-600 dark:text-gray-300 hover:text-blue-600 dark:hover:text-blue-400 font-medium text-sm transition-colors">
              {{ t('login') }}
            </RouterLink>
            <RouterLink to="/signup" class="px-4 py-2 rounded-full bg-gradient-to-r from-blue-600 to-purple-600 text-white text-sm font-medium hover:shadow-lg hover:opacity-90 transition-all transform hover:-translate-y-0.5">
              {{ t('signup') }}
            </RouterLink>
          </div>
          <div v-else>
            <button @click="authStore.logout" class="text-gray-600 dark:text-gray-300 hover:text-red-500 dark:hover:text-red-400 font-medium text-sm transition-colors">
              {{ t('logout') }}
            </button>
          </div>
        </div>
      </div>
    </div>
  </nav>
</template>
