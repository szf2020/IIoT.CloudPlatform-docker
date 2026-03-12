<template>
  <div class="login-container">
    <h2>IIoT Cloud Platform 登录</h2>
    <form @submit.prevent="handleLogin" class="login-form">
      <div class="form-group">
        <label>工号:</label>
        <input v-model="loginForm.employeeNo" type="text" placeholder="请输入工号" required />
      </div>
      <div class="form-group">
        <label>密码:</label>
        <input v-model="loginForm.password" type="password" placeholder="请输入密码" required />
      </div>
      <button type="submit" :disabled="loading">
        {{ loading ? '登录中...' : '登 录' }}
      </button>
    </form>
  </div>
</template>

<script setup lang="ts">
import { reactive, ref } from 'vue';
import { useRouter } from 'vue-router';
import { loginApi } from '../api/auth';
import type { LoginPayload } from '../api/auth';

const router = useRouter();
const loading = ref(false);

const loginForm = reactive<LoginPayload>({
  employeeNo: '',
  password: ''
});

const handleLogin = async () => {
  loading.value = true;
  try {
    // 1. 发起请求，刚才配好的 axios 拦截器会自动处理报错！
    const token = await loginApi(loginForm) as unknown as string;
    
    // 2. 登录成功，把 token 存入 localStorage
    localStorage.setItem('token', token);
    
    // 3. 提示并跳转到首页
    alert('登录成功！');
    router.push('/');
  } catch (error) {
    // 错误已经在 utils/http.ts 拦截器里通过 alert 弹出了，这里不需要额外处理
    console.log('登录拦截结束');
  } finally {
    loading.value = false;
  }
};
</script>

<style scoped>
.login-container {
  max-width: 400px;
  margin: 100px auto;
  padding: 30px;
  border: 1px solid #ddd;
  border-radius: 8px;
  box-shadow: 0 4px 12px rgba(0,0,0,0.1);
}
.form-group {
  margin-bottom: 20px;
  display: flex;
  flex-direction: column;
  text-align: left;
}
input {
  padding: 10px;
  margin-top: 5px;
  border: 1px solid #ccc;
  border-radius: 4px;
}
button {
  width: 100%;
  padding: 10px;
  background-color: #4CAF50;
  color: white;
  border: none;
  border-radius: 4px;
  cursor: pointer;
  font-size: 16px;
}
button:disabled {
  background-color: #9E9E9E;
}
</style>