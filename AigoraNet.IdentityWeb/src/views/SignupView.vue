<script setup lang="ts">
import AuthLayout from '@/layouts/AuthLayout.vue'
import BaseButton from '@/components/atoms/BaseButton.vue'
import BaseInput from '@/components/atoms/BaseInput.vue'
import FormGroup from '@/components/molecules/FormGroup.vue'
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import api from '@/api/axios'
import Swal from 'sweetalert2'
import { useI18n } from 'vue-i18n'

const email = ref('')
const password = ref('')
const nickname = ref('')
const loading = ref(false)
const router = useRouter()
const { t } = useI18n()

const handleSignup = async () => {
  loading.value = true
  try {
    // Using /auth/register as found in OpenAPI spec
    await api.post('/auth/register', {
      email: email.value,
      passwordHash: password.value, // Spec says passwordHash, usually means password in registration DTOs
      nickName: nickname.value,
      type: 0, // Assuming 0 is default member type
      createdBy: 'self'
    })
    
    Swal.fire({
      icon: 'success',
      title: t('signup') + ' Success',
      text: 'Please login with your new account.',
      confirmButtonText: 'OK'
    }).then(() => {
      router.push('/login')
    })
  } catch (error: any) {
    console.error(error)
    Swal.fire({
      icon: 'error',
      title: 'Signup Failed',
      text: error.response?.data?.message || 'An error occurred during signup.'
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
        {{ t('signup') }}
      </h2>
      <p class="mt-2 text-center text-sm text-gray-600">
        Or
        <RouterLink to="/login" class="font-medium text-blue-600 hover:text-blue-500">
          {{ t('login') }}
        </RouterLink>
      </p>
    </div>
    <form class="mt-8 space-y-6" @submit.prevent="handleSignup">
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
        <FormGroup label="Nickname" id="nickname">
          <BaseInput
            id="nickname"
            v-model="nickname"
            type="text"
            required
            placeholder="Nickname"
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
          <span v-else>{{ t('signup') }}</span>
        </BaseButton>
      </div>
    </form>
  </AuthLayout>
</template>
