<script setup lang="ts">
import MainLayout from '@/layouts/MainLayout.vue'
import { useI18n } from 'vue-i18n'

const { t } = useI18n()

</script>

<template>
  <MainLayout>
    <!-- Hero Section -->
    <div class="relative pt-20 pb-16 sm:pt-24 sm:pb-20 lg:pb-28 overflow-hidden">
      <div class="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 relative z-10">
        <div class="lg:grid lg:grid-cols-12 lg:gap-8 items-center">
          
          <!-- Text Content -->
          <div class="sm:text-center md:max-w-2xl md:mx-auto lg:col-span-6 lg:text-left">
            <div class="inline-flex items-center px-3 py-1 rounded-full border border-blue-500/30 bg-blue-500/10 text-blue-600 dark:text-blue-400 text-xs font-mono mb-6">
              <span class="flex h-2 w-2 rounded-full bg-blue-500 mr-2 animate-pulse"></span>
              {{ t('home_hero_badge') }}
            </div>
            <h1 class="text-4xl tracking-tight font-extrabold text-gray-900 dark:text-white sm:text-5xl md:text-6xl mb-6">
              <span class="block">{{ t('home_hero_title_1') }}</span>
              <span class="block text-transparent bg-clip-text bg-gradient-to-r from-blue-600 to-purple-600 dark:from-blue-400 dark:to-purple-400">{{ t('home_hero_title_2') }}</span>
            </h1>
            <p class="mt-3 text-base text-gray-500 dark:text-gray-400 sm:mt-5 sm:text-lg sm:max-w-xl sm:mx-auto md:mt-5 md:text-xl lg:mx-0 font-light">
              {{ t('home_hero_desc') }}
            </p>
            <div class="mt-8 sm:max-w-lg sm:mx-auto sm:text-center lg:text-left lg:mx-0 flex flex-col sm:flex-row gap-4">
              <RouterLink to="/signup" class="inline-flex items-center justify-center px-8 py-3 border border-transparent text-base font-medium rounded-md text-white bg-blue-600 hover:bg-blue-700 transition-all shadow-[0_0_20px_rgba(37,99,235,0.5)] hover:shadow-[0_0_30px_rgba(37,99,235,0.7)]">
                {{ t('home_hero_btn_start') }}
              </RouterLink>
              <RouterLink to="/document" class="inline-flex items-center justify-center px-8 py-3 border border-gray-300 dark:border-gray-700 text-base font-medium rounded-md text-gray-700 dark:text-gray-300 bg-white dark:bg-gray-800 hover:bg-gray-50 dark:hover:bg-gray-700 transition-all font-mono">
                {{ t('home_hero_btn_docs') }}
              </RouterLink>
            </div>
          </div>

          <!-- Terminal Visualization -->
          <div class="mt-12 relative sm:max-w-lg sm:mx-auto lg:mt-0 lg:max-w-none lg:mx-0 lg:col-span-6 lg:flex lg:items-center">
            <div class="relative mx-auto w-full rounded-lg shadow-lg lg:max-w-md">
              <div class="terminal-window w-full">
                <div class="terminal-header">
                  <div class="terminal-dot red"></div>
                  <div class="terminal-dot yellow"></div>
                  <div class="terminal-dot green"></div>
                </div>
                <div class="terminal-body font-mono text-sm">
                  <div class="mb-4">
                    <span class="text-green-400">➜</span> <span class="text-blue-400">~</span> <span class="text-gray-400">curl</span> -X POST https://api.aigora.net/mcp
                  </div>
                  <div class="pl-4 border-l-2 border-gray-700 mb-4">
                    <span class="code-keyword">"agent_id"</span>: <span class="code-string">"agent-007"</span>,<br>
                    <span class="code-keyword">"keyword"</span>: <span class="code-string">"analyze_data"</span>
                  </div>
                  <div class="mb-2">
                    <span class="text-green-400">➜</span> <span class="text-blue-400">~</span> <span class="text-gray-400">Response:</span>
                  </div>
                  <div class="pl-4 text-green-300">
                    {<br>
                    &nbsp;&nbsp;<span class="code-keyword">"status"</span>: <span class="code-string">"success"</span>,<br>
                    &nbsp;&nbsp;<span class="code-keyword">"prompt"</span>: <span class="code-string">"Analyze the following dataset using statistical methods..."</span><br>
                    }
                  </div>
                  <div class="mt-4 animate-pulse">
                    <span class="text-green-400">➜</span> <span class="text-blue-400">~</span> <span class="w-2 h-4 bg-gray-400 inline-block align-middle"></span>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- Feature Flow Section -->
    <div class="py-24 border-t border-gray-200 dark:border-gray-800 relative">
      <div class="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
        <div class="text-center mb-16">
          <h2 class="text-base text-blue-600 dark:text-blue-400 font-semibold tracking-wide uppercase font-mono">{{ t('home_workflow_badge') }}</h2>
          <p class="mt-2 text-3xl leading-8 font-extrabold tracking-tight text-gray-900 dark:text-white sm:text-4xl">
            {{ t('home_workflow_title') }}
          </p>
        </div>

        <div class="grid grid-cols-1 md:grid-cols-3 gap-12">
          <!-- Step 1 -->
          <div class="relative p-6 rounded-2xl border border-gray-200 dark:border-gray-800 bg-white/50 dark:bg-gray-900/50 backdrop-blur-sm hover:border-blue-500/50 transition-colors group">
            <div class="absolute -top-6 left-6 w-12 h-12 rounded-xl bg-blue-600 flex items-center justify-center text-white font-bold text-xl shadow-lg group-hover:scale-110 transition-transform">1</div>
            <h3 class="mt-8 text-xl font-bold text-gray-900 dark:text-white mb-4">{{ t('home_step1_title') }}</h3>
            <p class="text-gray-500 dark:text-gray-400">
              {{ t('home_step1_desc') }}
            </p>
          </div>

          <!-- Step 2 -->
          <div class="relative p-6 rounded-2xl border border-gray-200 dark:border-gray-800 bg-white/50 dark:bg-gray-900/50 backdrop-blur-sm hover:border-purple-500/50 transition-colors group">
            <div class="absolute -top-6 left-6 w-12 h-12 rounded-xl bg-purple-600 flex items-center justify-center text-white font-bold text-xl shadow-lg group-hover:scale-110 transition-transform">2</div>
            <h3 class="mt-8 text-xl font-bold text-gray-900 dark:text-white mb-4">{{ t('home_step2_title') }}</h3>
            <p class="text-gray-500 dark:text-gray-400">
              {{ t('home_step2_desc') }}
            </p>
          </div>

          <!-- Step 3 -->
          <div class="relative p-6 rounded-2xl border border-gray-200 dark:border-gray-800 bg-white/50 dark:bg-gray-900/50 backdrop-blur-sm hover:border-green-500/50 transition-colors group">
            <div class="absolute -top-6 left-6 w-12 h-12 rounded-xl bg-green-600 flex items-center justify-center text-white font-bold text-xl shadow-lg group-hover:scale-110 transition-transform">3</div>
            <h3 class="mt-8 text-xl font-bold text-gray-900 dark:text-white mb-4">{{ t('home_step3_title') }}</h3>
            <p class="text-gray-500 dark:text-gray-400">
              {{ t('home_step3_desc') }}
            </p>
          </div>
        </div>
      </div>
    </div>
  </MainLayout>
</template>
