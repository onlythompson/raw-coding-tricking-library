﻿<template>
  <item-content-layout>
    <template v-slot:content>
      <submission-feed :content-endpoint="`/api/tricks/${trick.slug}/submissions`"/>
    </template>
    <template v-slot:item="{close}">
      <trick-info-card :trick="trick" :close="close"/>
    </template>
  </item-content-layout>
</template>

<script>
import {mapState} from 'vuex';
import ItemContentLayout from "@/components/item-content-layout";
import Submission from "@/components/submission";
import TrickInfoCard from "@/components/trick-info-card";
import SubmissionFeed from "@/components/submission-feed";

export default {
  components: {SubmissionFeed, TrickInfoCard, Submission, ItemContentLayout},
  computed: {
    ...mapState('library', ['dictionary']),
    trick() {
      return this.dictionary.tricks[this.$route.params.trick]
    }
  },
  head() {
    if (!this.trick) return {}

    return {
      title: this.trick.name,
      meta: [
        {hid: 'description', name: 'description', content: this.trick.description}
      ]
    }
  }
}
</script>

<style scoped></style>
