<script setup lang="ts">
import MainLayout from '@/layouts/MainLayout.vue'
import { useI18n } from 'vue-i18n'

const { t } = useI18n()
</script>

<template>
  <MainLayout>
    <div class="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-12">
      <h1 class="text-3xl font-bold text-gray-900 mb-8">{{ t('document') }}</h1>
      
      <div class="prose prose-blue max-w-none">
        <h2>Getting Started</h2>
        <p>
          Welcome to AIGORA, the MCP Server that bridges your agents with powerful prompt templates.
        </p>
        
        <h3>1. Sign Up</h3>
        <p>Create an account to manage your own prompt keys.</p>
        
        <h3>2. Create a Key</h3>
        <p>
          Go to the Key Management dashboard and create a new key. Map a simple keyword (e.g., <code>translate_ko</code>) to a complex prompt.
        </p>
        
        <h3>3. Connect your Agent</h3>
        <p>
          Configure your MCP agent to use the AIGORA server. When your agent sends the keyword, AIGORA will intercept it and replace it with your defined prompt.
        </p>
        
        <h3>API Reference</h3>
        <p>
          Please refer to the <a href="https://api.aigora.net/scalar/v1" target="_blank" class="text-blue-600 hover:underline">API Documentation</a> for detailed endpoint information.
        </p>
      </div>
    </div>
  </MainLayout>
</template>
