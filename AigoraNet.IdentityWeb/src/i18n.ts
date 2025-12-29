import { createI18n } from 'vue-i18n'
import en from './locales/en.json'
import ko from './locales/ko.json'
import ja from './locales/ja.json'
import zh from './locales/zh.json'

const getBrowserLocale = (): string => {
    if (typeof navigator === 'undefined') {
        return 'ko'
    }
    const navigatorLocale = navigator.language
    if (!navigatorLocale) {
        return 'ko'
    }
    const firstPart = navigatorLocale.split('-')[0]
    return firstPart || 'ko'
}

const getLocale = (): string => {
    const savedLocale = localStorage.getItem('locale')
    if (savedLocale) {
        return savedLocale
    }
    const browserLocale = getBrowserLocale()
    // Check if browser locale is supported, otherwise default to 'ko'
    const supportedLocales = ['en', 'ko', 'ja', 'zh']
    return supportedLocales.includes(browserLocale) ? browserLocale : 'ko'
}

const i18n = createI18n({
    legacy: false,
    locale: getLocale(),
    fallbackLocale: 'ko',
    messages: {
        en,
        ko,
        ja,
        zh
    }
})

export default i18n
