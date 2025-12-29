<script setup lang="ts">
import TheNavbar from '@/components/organisms/TheNavbar.vue'
import TheFooter from '@/components/organisms/TheFooter.vue'
</script>

<template>
  <div class="app-layout flex flex-col relative transition-colors duration-200">
    <!-- Background Gradient Mesh (Removed for Grid) -->
    <!-- <div class="absolute inset-0 overflow-hidden pointer-events-none">...</div> -->

    <TheNavbar />
    <main class="flex-grow pt-16 relative z-10">
      <slot />
    </main>
    <TheFooter />
  </div>
</template>
