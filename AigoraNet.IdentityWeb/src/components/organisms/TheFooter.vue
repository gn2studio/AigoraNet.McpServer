<script setup lang="ts">
import { useI18n } from 'vue-i18n'

const { t } = useI18n()
</script>

<template>
  <footer class="bg-white dark:bg-gray-900 border-t border-gray-200 dark:border-gray-800 transition-colors duration-200">
    <div class="max-w-7xl mx-auto py-8 px-4 sm:px-6 lg:px-8">
      <div class="md:flex md:items-center md:justify-between">
        <div class="flex justify-center space-x-6 md:order-2">
          <RouterLink to="/terms" class="text-gray-400 hover:text-gray-500 dark:hover:text-gray-300 text-sm">
            {{ t('terms_of_service') }}
          </RouterLink>
          <RouterLink to="/privacy" class="text-gray-400 hover:text-gray-500 dark:hover:text-gray-300 text-sm">
            {{ t('privacy_policy') }}
          </RouterLink>
        </div>
        <div class="mt-8 md:mt-0 md:order-1">
          <p class="text-center text-base text-gray-400">
            &copy; 2025 AIGORA. All rights reserved.
          </p>
        </div>
      </div>
    </div>
  </footer>
</template>
