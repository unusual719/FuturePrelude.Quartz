<template>
  <!-- 动态背景粒子效果 -->
  <div class="bg-particles" ref="particlesRef"></div>
  <div class="grid-bg"></div>
  <div class="decorative-side"></div>

  <!-- 登录卡片 -->
  <div class="login-container">
    <LoginForm />
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import LoginForm from '../components/LoginForm.vue'
import '../styles/login.css'

const particlesRef = ref(null)

// 生成粒子背景
const createParticles = () => {
  const container = particlesRef.value
  if (!container) return

  for (let i = 0; i < 40; i++) {
    const particle = document.createElement('div')
    particle.classList.add('particle')
    const size = Math.random() * 60 + 20
    particle.style.width = size + 'px'
    particle.style.height = size + 'px'
    particle.style.left = Math.random() * 100 + '%'
    particle.style.top = Math.random() * 100 + '%'
    particle.style.animationDelay = Math.random() * 20 + 's'
    particle.style.animationDuration = (Math.random() * 15 + 10) + 's'
    container.appendChild(particle)
  }
}

onMounted(() => {
  createParticles()
})
</script>