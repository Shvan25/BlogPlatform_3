// Глобальный объект для управления аутентификацией
window.AuthManager = {
    // Сохранить токен
    saveToken: function (token) {
        if (!token) return false;

        // Сохраняем в localStorage
        localStorage.setItem('token', token);

        // Сохраняем в sessionStorage
        sessionStorage.setItem('token', token);

        // Сохраняем в куки (на 1 день)
        const expires = new Date();
        expires.setDate(expires.getDate() + 1);
        document.cookie = `auth_token=${token}; expires=${expires.toUTCString()}; path=/; samesite=strict`;

        console.log('Токен сохранен');
        return true;
    },

    // Получить токен
    getToken: function () {
        return localStorage.getItem('token') ||
            sessionStorage.getItem('token') ||
            this.getCookie('auth_token');
    },

    // Получить данные пользователя
    getUser: function () {
        try {
            const userStr = localStorage.getItem('user');
            return userStr ? JSON.parse(userStr) : null;
        } catch (e) {
            return null;
        }
    },

    // Проверить авторизацию
    isAuthenticated: function () {
        return !!this.getToken();
    },

    // Проверить роль
    hasRole: function (role) {
        const user = this.getUser();
        return user && user.roles && user.roles.includes(role);
    },

    // Проверить админа или модератора
    isAdminOrModerator: function () {
        return this.hasRole('Admin') || this.hasRole('Moderator');
    },

    // Выход
    logout: function () {
        localStorage.removeItem('token');
        localStorage.removeItem('user');
        sessionStorage.removeItem('token');
        document.cookie = 'auth_token=; expires=Thu, 01 Jan 1970 00:00:00 UTC; path=/;';
        window.location.href = '/';
    },

    // Настроить заголовки для fetch
    setupFetchInterceptor: function () {
        const originalFetch = window.fetch;
        const self = this;

        window.fetch = function (url, options = {}) {
            // Добавляем токен к запросам на наш API
            if (typeof url === 'string' &&
                (url.startsWith('/api/') || url.includes('/api/'))) {

                const token = self.getToken();
                if (token) {
                    options.headers = options.headers || {};
                    options.headers['Authorization'] = 'Bearer ' + token;
                }
            }

            return originalFetch(url, options);
        };
    },

    // Вспомогательная функция для получения куки
    getCookie: function (name) {
        const value = `; ${document.cookie}`;
        const parts = value.split(`; ${name}=`);
        if (parts.length === 2) return parts.pop().split(';').shift();
        return null;
    }
};

// Инициализация при загрузке страницы
document.addEventListener('DOMContentLoaded', function () {
    // Настраиваем перехватчик fetch
    AuthManager.setupFetchInterceptor();

    // Обновляем навигацию
    updateNavigation();

    // Проверяем токен при загрузке каждой страницы
    checkAuthAndRedirect();
});

// Обновить навигацию
function updateNavigation() {
    const token = AuthManager.getToken();
    const user = AuthManager.getUser();

    // Обновляем видимость элементов навигации
    document.querySelectorAll('.login-link').forEach(link => {
        link.style.display = token ? 'none' : 'block';
    });

    document.querySelectorAll('.logout-link').forEach(link => {
        link.style.display = token ? 'block' : 'none';
    });

    // Обновляем административные ссылки
    document.querySelectorAll('.admin-link').forEach(link => {
        if (token && user && user.roles) {
            const isAdminOrModerator = user.roles.some(role =>
                role === 'Admin' || role === 'Moderator'
            );
            link.style.display = isAdminOrModerator ? 'block' : 'none';
        } else {
            link.style.display = 'none';
        }
    });

    // Обновляем имя пользователя
    document.querySelectorAll('.user-name').forEach(span => {
        if (token && user) {
            span.textContent = user.username || user.email;
            span.style.display = 'inline';
        } else {
            span.style.display = 'none';
        }
    });

    // Обновляем роль пользователя
    document.querySelectorAll('.user-role').forEach(span => {
        if (token && user && user.roles) {
            if (user.roles.includes('Admin')) {
                span.textContent = 'Администратор';
                span.className = 'badge bg-danger user-role';
            } else if (user.roles.includes('Moderator')) {
                span.textContent = 'Модератор';
                span.className = 'badge bg-warning user-role';
            } else {
                span.textContent = 'Пользователь';
                span.className = 'badge bg-secondary user-role';
            }
            span.style.display = 'inline';
        } else {
            span.style.display = 'none';
        }
    });
}

// Проверить авторизацию и перенаправить если нужно
function checkAuthAndRedirect() {
    const protectedPages = ['/Users', '/Role', '/Comments'];
    const currentPath = window.location.pathname;

    // Если страница требует авторизации
    if (protectedPages.some(page => currentPath.startsWith(page))) {
        if (!AuthManager.isAuthenticated()) {
            window.location.href = '/Auth/Login?returnUrl=' + encodeURIComponent(currentPath);
            return;
        }

        // Проверяем права для конкретных страниц
        if (currentPath.startsWith('/Users') || currentPath.startsWith('/Role') || currentPath.startsWith('/Comments')) {
            if (!AuthManager.isAdminOrModerator()) {
                alert('У вас нет прав для доступа к этой странице');
                window.location.href = '/';
            }
        }
    }
}
// Функция для проверки работы API
window.testApi = async function () {
    console.log('=== Тестирование API ===');

    try {
        // 1. Проверка публичного эндпоинта
        console.log('1. Тестируем публичный эндпоинт...');
        const publicResponse = await fetch('/api/test/public');
        const publicData = await publicResponse.json();
        console.log('Публичный эндпоинт:', publicData);

        // 2. Проверка аутентификации
        console.log('2. Тестируем аутентификацию...');
        const authResponse = await fetch('/api/test/auth');
        if (authResponse.ok) {
            const authData = await authResponse.json();
            console.log('Аутентификация успешна:', authData);
        } else {
            console.log('Ошибка аутентификации:', authResponse.status);
        }

        // 3. Проверка роли администратора
        console.log('3. Тестируем права администратора...');
        const adminResponse = await fetch('/api/test/admin');
        if (adminResponse.ok) {
            const adminData = await adminResponse.json();
            console.log('Права администратора:', adminData);
        } else {
            console.log('Нет прав администратора:', adminResponse.status);
        }

        // 4. Проверка получения пользователей
        console.log('4. Тестируем получение пользователей...');
        const usersResponse = await fetch('/api/users');
        if (usersResponse.ok) {
            const usersData = await usersResponse.json();
            console.log('Пользователи получены:', usersData.length, 'записей');
        } else {
            console.log('Ошибка получения пользователей:', usersResponse.status);
        }

        // 5. Проверка получения статей
        console.log('5. Тестируем получение статей...');
        const articlesResponse = await fetch('/api/articles');
        if (articlesResponse.ok) {
            const articlesData = await articlesResponse.json();
            console.log('Статьи получены:', articlesData.length, 'записей');
        } else {
            console.log('Ошибка получения статей:', articlesResponse.status);
        }

        // 6. Проверка получения тегов
        console.log('6. Тестируем получение тегов...');
        const tagsResponse = await fetch('/api/tags');
        if (tagsResponse.ok) {
            const tagsData = await tagsResponse.json();
            console.log('Теги получены:', tagsData.length, 'записей');
        } else {
            console.log('Ошибка получения тегов:', tagsResponse.status);
        }

        console.log('=== Тестирование завершено ===');

    } catch (error) {
        console.error('Ошибка при тестировании API:', error);
    }
};

// Глобальная функция для выхода
window.logout = function () {
    if (confirm('Вы уверены, что хотите выйти?')) {
        AuthManager.logout();
    }
};
