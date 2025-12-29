<template>
  <div class="bg-white dark:bg-gray-800 shadow-md rounded-lg overflow-hidden border border-transparent dark:border-gray-700 transition-colors duration-200">
    <div v-if="$slots.header" class="px-6 py-4 border-b border-gray-200 dark:border-gray-700">
      <slot name="header" />
    </div>
    <div class="p-6 text-gray-900 dark:text-gray-100">
      <slot />
    </div>
    <div v-if="$slots.footer" class="px-6 py-4 bg-gray-50 dark:bg-gray-700 border-t border-gray-200 dark:border-gray-600">
      <slot name="footer" />
    </div>
  </div>
</template>
