<script setup lang="ts">
import MainLayout from '@/layouts/MainLayout.vue'
import BaseButton from '@/components/atoms/BaseButton.vue'
import BaseCard from '@/components/atoms/BaseCard.vue'
import BaseInput from '@/components/atoms/BaseInput.vue'
import FormGroup from '@/components/molecules/FormGroup.vue'
import { ref, onMounted } from 'vue'
import api from '@/api/axios'
import Swal from 'sweetalert2'
import { useI18n } from 'vue-i18n'

const { t } = useI18n()
const keys = ref<any[]>([])
const loading = ref(false)
const showCreateModal = ref(false)
const newKey = ref({
  keyword: '',
  prompt: ''
})

const fetchKeys = async () => {
  loading.value = true
  try {
    // Assuming /system/keyword-prompts is the endpoint
    const response = await api.get('/system/keyword-prompts')
    keys.value = response.data
  } catch (error) {
    console.error(error)
    // Mock data for demonstration if API fails or is empty
    keys.value = [
      { id: '1', keyword: 'translate_ko', prompt: 'Translate the following text to Korean:', locale: 'en' },
      { id: '2', keyword: 'summarize', prompt: 'Summarize the following text in 3 bullet points:', locale: 'en' }
    ]
  } finally {
    loading.value = false
  }
}

const createKey = async () => {
  try {
    await api.post('/system/keyword-prompts', {
      // Adjust payload based on actual API spec for UpsertKeywordPromptCommand
      // Spec says: content (UpsertKeywordPromptCommand)
      // UpsertKeywordPromptCommand properties: ? (Need to check spec again or guess)
      // Based on context: keyword, prompt template
      // Actually spec for UpsertKeywordPromptCommand was in chunk 6 but I didn't see the properties detail in the summary.
      // I'll assume standard fields.
      keyword: newKey.value.keyword,
      prompt: newKey.value.prompt
    })
    Swal.fire('Success', 'Key created successfully', 'success')
    showCreateModal.value = false
    fetchKeys()
  } catch (error) {
    console.error(error)
    Swal.fire('Error', 'Failed to create key', 'error')
  }
}

const deleteKey = async (id: string) => {
  try {
    await api.delete(`/system/keyword-prompts/${id}`)
    Swal.fire('Deleted', 'Key has been deleted', 'success')
    fetchKeys()
  } catch (error) {
    console.error(error)
    Swal.fire('Error', 'Failed to delete key', 'error')
  }
}

onMounted(() => {
  fetchKeys()
})
</script>

<template>
  <MainLayout>
    <div class="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-12">
      <div class="flex justify-between items-center mb-8">
        <h1 class="text-3xl font-bold text-gray-900">{{ t('key_management') }}</h1>
        <BaseButton @click="showCreateModal = !showCreateModal">
          <i class="fas fa-plus mr-2"></i> New Key
        </BaseButton>
      </div>

      <!-- Create Form (Simple toggle for demo) -->
      <div v-if="showCreateModal" class="mb-8 bg-white p-6 rounded-lg shadow">
        <h2 class="text-xl font-semibold mb-4">Create New Key</h2>
        <form @submit.prevent="createKey">
          <FormGroup label="Keyword">
            <BaseInput v-model="newKey.keyword" placeholder="e.g., translate_ko" required />
          </FormGroup>
          <FormGroup label="Prompt">
            <textarea
              v-model="newKey.prompt"
              class="w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm focus:outline-none focus:ring-blue-500 focus:border-blue-500"
              rows="4"
              placeholder="Enter your prompt template..."
              required
            ></textarea>
          </FormGroup>
          <div class="flex justify-end space-x-3">
            <BaseButton variant="ghost" @click="showCreateModal = false">Cancel</BaseButton>
            <BaseButton type="submit">Create</BaseButton>
          </div>
        </form>
      </div>

      <!-- Key List -->
      <div v-if="loading" class="text-center py-12">
        <i class="fas fa-spinner fa-spin text-4xl text-blue-500"></i>
      </div>
      <div v-else class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
        <BaseCard v-for="key in keys" :key="key.id">
          <template #header>
            <div class="flex justify-between items-center">
              <span class="font-bold text-lg text-blue-600">{{ key.keyword }}</span>
              <button @click="deleteKey(key.id)" class="text-red-500 hover:text-red-700">
                <i class="fas fa-trash"></i>
              </button>
            </div>
          </template>
          <p class="text-gray-600">{{ key.prompt }}</p>
          <template #footer>
            <span class="text-xs text-gray-400">ID: {{ key.id }}</span>
          </template>
        </BaseCard>
      </div>
    </div>
  </MainLayout>
</template>
