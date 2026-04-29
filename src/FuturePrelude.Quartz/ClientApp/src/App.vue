<template>
    <router-view />
    <ToastContainer />
    <ConfirmModal />
    <Loading />
</template>

<script setup>
    import ToastContainer from './components/ToastContainer.vue'
    import ConfirmModal from './components/ConfirmModal.vue'
    import Loading from './components/Loading.vue'
</script>