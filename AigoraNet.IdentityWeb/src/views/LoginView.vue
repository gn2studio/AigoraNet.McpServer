<script setup lang="ts">
import AuthLayout from '@/layouts/AuthLayout.vue'
import BaseButton from '@/components/atoms/BaseButton.vue'
import BaseInput from '@/components/atoms/BaseInput.vue'
import FormGroup from '@/components/molecules/FormGroup.vue'
import { ref } from 'vue'
import { useAuthStore } from '@/stores/auth'
import { useRouter } from 'vue-router'
import api from '@/api/axios'
import Swal from 'sweetalert2'
import { useI18n } from 'vue-i18n'

const email = ref('')
const password = ref('')
const loading = ref(false)
const authStore = useAuthStore()
const router = useRouter()
const { t } = useI18n()

const handleLogin = async () => {
  loading.value = true
  try {
    // Assuming POST /auth/login based on standard patterns, or /auth/tokens
    // If API spec is different, this needs adjustment.
    // For now, trying /auth/login
    const response = await api.post('/auth/login', {
      email: email.value,
      password: password.value
    })
    
    // Assuming response contains token
    const token = response.data.token || response.data.accessToken
    authStore.setToken(token)
    
    Swal.fire({
      icon: 'success',
      title: t('login') + ' Success',
      showConfirmButton: false,
      timer: 1500
    })
    router.push('/')
  } catch (error: any) {
    console.error(error)
    Swal.fire({
      icon: 'error',
      title: 'Login Failed',
      text: error.response?.data?.message || 'Please check your credentials.'
    })
  } finally {
    loading.value = false
  }
}
</script>

<template>
  <AuthLayout>
    <div>
      <h2 class="mt-6 text-center text-3xl font-extrabold text-gray-900">
        {{ t('login') }}
      </h2>
      <p class="mt-2 text-center text-sm text-gray-600">
        Or
        <RouterLink to="/signup" class="font-medium text-blue-600 hover:text-blue-500">
          {{ t('signup') }}
        </RouterLink>
      </p>
    </div>
    <form class="mt-8 space-y-6" @submit.prevent="handleLogin">
      <div class="rounded-md shadow-sm -space-y-px">
        <FormGroup label="Email" id="email-address">
          <BaseInput
            id="email-address"
            v-model="email"
            type="email"
            required
            placeholder="Email address"
          />
        </FormGroup>
        <FormGroup label="Password" id="password">
          <BaseInput
            id="password"
            v-model="password"
            type="password"
            required
            placeholder="Password"
          />
        </FormGroup>
      </div>

      <div>
        <BaseButton type="submit" block :disabled="loading">
          <span v-if="loading">Loading...</span>
          <span v-else>{{ t('login') }}</span>
        </BaseButton>
      </div>
    </form>
  </AuthLayout>
</template>
