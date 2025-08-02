// 🚀 MODERN QR MENU ENGINE
class QRMenuEngine {
    constructor() {
        this.menuData = null;
        this.currentView = 'categories';
        this.currentCategory = null;
        this.customerData = null;
        this.touchStartX = 0;
        this.touchStartY = 0;
        this.loadingStates = {
            menu: false,
            suggestions: false  // ✅ weather'ı kaldırdım
        };

        this.init();
    }

    async init() {
        console.log('🚀 QR Menu Engine initializing...');

        try {
            // Paralel yükleme
            await Promise.all([
                this.loadMenuData(),
                this.loadSmartSuggestions(),
                this.initializeUI()
            ]);

            await this.hideLoader();
            this.scheduleSmartSuggestion();

        } catch (error) {
            console.error('❌ Initialization error:', error);
            this.showError('Menü yüklenirken hata oluştu. Lütfen sayfayı yenileyin.');
        }
    }

    // 📦 MENU DATA LOADING - CLEAN VERSION
    async loadMenuData() {
        try {
            const response = await fetch('/api/qr-menu/menu-data');

            if (!response.ok) throw new Error('Menu data load failed');

            const result = await response.json();

            if (!result.success) throw new Error(result.message);

            this.menuData = result.data;
            this.loadingStates.menu = true;

            // Kategorileri render et
            this.renderCategories();

        } catch (error) {
            console.error('❌ Menu data error:', error);
            throw error;
        }
    }
    // 🧠 SMART SUGGESTIONS - DEBUG
    async loadSmartSuggestions() {
        try {
            console.log('🧠 Loading smart suggestions...');
            const response = await fetch('/api/qr-menu/smart-suggestions');

            console.log('🧠 Suggestions response status:', response.status);

            if (response.ok) {
                const result = await response.json();
                console.log('🧠 Suggestions result:', result);

                if (result.success) {
                    this.updateWeatherDisplay(result.data.weather);
                    this.prepareSuggestion(result.data.suggestion);
                    console.log('🧠 Weather data:', result.data.weather);
                    console.log('🧠 Suggestion data:', result.data.suggestion);
                }
            }

            this.loadingStates.suggestions = true;

        } catch (error) {
            console.log('⚠️ Smart suggestions failed:', error);
            this.loadingStates.suggestions = true;
        }
    }

    // 🎨 UI INITIALIZATION
    async initializeUI() {
        this.startClock();
        this.initializeTouchEvents();
        this.initializeIntersectionObserver();

        // Escape key listener
        document.addEventListener('keydown', (e) => {
            if (e.key === 'Escape') {
                this.closeAllModals();
            }
        });

        console.log('✅ UI initialized');
    }

    // ⏰ CLOCK
    startClock() {
        const updateClock = () => {
            const now = new Date();
            const timeString = now.toLocaleTimeString('tr-TR', {
                hour: '2-digit',
                minute: '2-digit'
            });

            const timeElement = document.getElementById('currentTime');
            if (timeElement) {
                timeElement.textContent = timeString;
            }
        };

        updateClock();
        setInterval(updateClock, 60000); // Her dakika güncelle
    }

    // 🌤️ WEATHER DISPLAY
    updateWeatherDisplay(weatherData) {
        if (!weatherData) return;

        const weatherDisplay = document.getElementById('weatherDisplay');
        if (!weatherDisplay) return;

        const temp = Math.round(weatherData.Temperature || 20);
        const condition = weatherData.Condition || 'clear';

        let icon = '🌤️';
        if (condition.toLowerCase().includes('rain')) icon = '🌧️';
        else if (condition.toLowerCase().includes('cloud')) icon = '☁️';
        else if (condition.toLowerCase().includes('clear')) icon = '☀️';
        else if (condition.toLowerCase().includes('snow')) icon = '❄️';

        weatherDisplay.innerHTML = `
            <span class="weather-icon">${icon}</span>
            <span class="weather-temp">${temp}°</span>
        `;
    }

    // 📂 RENDER CATEGORIES - CLEAN VERSION
    renderCategories() {
        const grid = document.getElementById('categoriesGrid');
        if (!grid || !this.menuData?.categories) return;

        const categoriesHTML = this.menuData.categories.map(category => {
            const name = category.name || category.Name;
            const displayName = category.displayName || category.DisplayName || name;
            const productCount = category.productCount || category.ProductCount || 0;
            const icon = category.icon || category.Icon || '🍽️';
            const randomImage = category.randomImage || category.RandomImage;

            return `
            <div class="category-card fade-in" onclick="QRMenuApp.selectCategory('${name}')" data-category="${name}">
                <div class="category-image">
                    ${randomImage ?
                    `<img src="${randomImage}" alt="${displayName}" loading="lazy" onerror="this.style.display='none'">` :
                    `<span style="font-size: 4rem;">${icon}</span>`
                }
                </div>
                <div class="category-info">
                    <h3 class="category-name">${displayName}</h3>
                    <div class="category-meta">
                        <span class="product-count">${productCount} ürün</span>
                        <span class="category-icon">${icon}</span>
                    </div>
                </div>
            </div>
        `;
        }).join('');

        grid.innerHTML = categoriesHTML;
    }

    // 🍽️ SELECT CATEGORY - CLEAN VERSION
    async selectCategory(categoryName) {
        try {
            this.currentCategory = categoryName;

            // Kategori ürünlerini filtrele
            const categoryProducts = this.menuData.products.filter(product => {
                const productCategory = product.categoryName || product.CategoryName;
                return productCategory === categoryName;
            });

            this.renderProducts(categoryProducts);
            this.showProductsView();

        } catch (error) {
            console.error('❌ Category selection error:', error);
            this.showError('Kategori yüklenirken hata oluştu.');
        }
    }

    // 🍽️ RENDER PRODUCTS - FİX VERSION
    renderProducts(products) {
        const grid = document.getElementById('productsGrid');
        const categoryTitle = document.getElementById('currentCategoryName');
        const productCount = document.getElementById('productCount');

        if (!grid) return;

        // Header bilgilerini güncelle
        if (categoryTitle) categoryTitle.textContent = this.currentCategory;
        if (productCount) productCount.textContent = `${products.length} ürün`;

        const productsHTML = products.map(product => {
            // Field isimlerini normalize et (backend küçük harf gönderiyor)
            const productId = product.id || product.Id;
            const productName = product.name || product.Name || 'İsimsiz Ürün';
            const productDescription = product.description || product.Description;
            const productPrice = product.price || product.Price || 0;
            const productImages = product.images || product.Images || [];
            const hasCampaign = product.hasCampaign || product.HasCampaign;
            const campaignCaption = product.campaignCaption || product.CampaignCaption;
            const hasKufePoints = product.hasKufePoints || product.HasKufePoints;
            const kufePoints = product.kufePoints || product.KufePoints || 0;

            return `
            <div class="product-card fade-in" onclick="QRMenuApp.showProductModal('${productId}')" data-product-id="${productId}">
                <div class="product-image">
                    ${productImages && productImages.length > 0 ?
                    `<img src="${productImages[0].Original || productImages[0].original}" alt="${productName}" loading="lazy" onerror="this.style.display='none'">` :
                    `<div style="display: flex; align-items: center; justify-content: center; height: 100%; font-size: 3rem; color: var(--accent);">🍽️</div>`
                }
                    <div class="product-badges">
                        ${hasCampaign ? `<span class="badge campaign">${campaignCaption || 'Kampanya'}</span>` : ''}
                        ${hasKufePoints && kufePoints > 0 ? `<span class="badge points">+${kufePoints} puan</span>` : ''}
                    </div>
                </div>
                <div class="product-info">
                    <h3 class="product-name">${productName}</h3>
                    ${productDescription ? `<p class="product-description">${productDescription}</p>` : ''}
                    <div class="product-footer">
                        <span class="product-price">₺${Number(productPrice).toFixed(2)}</span>
                        ${hasKufePoints && kufePoints > 0 ?
                    `<span class="product-points"><i class="fas fa-star"></i> +${kufePoints}</span>` :
                    ''
                }
                    </div>
                </div>
            </div>
        `;
        }).join('');

        grid.innerHTML = productsHTML;

        // Lazy loading için images'ları observe et
        this.observeImages();
    }

    // 👁️ INTERSECTION OBSERVER (Lazy Loading)
    initializeIntersectionObserver() {
        this.imageObserver = new IntersectionObserver((entries) => {
            entries.forEach(entry => {
                if (entry.isIntersecting) {
                    const img = entry.target;
                    if (img.dataset.src) {
                        img.src = img.dataset.src;
                        img.removeAttribute('data-src');
                        this.imageObserver.unobserve(img);
                    }
                }
            });
        }, {
            rootMargin: '50px'
        });
    }

    observeImages() {
        const images = document.querySelectorAll('img[data-src]');
        images.forEach(img => this.imageObserver.observe(img));
    }

    // 🖼️ PRODUCT MODAL
    async showProductModal(productId) {
        try {
            const product = this.menuData.products.find(p => (p.id || p.Id) === productId);
            if (!product) return;

            const modal = document.getElementById('productModal');
            const modalBody = document.getElementById('productModalBody');

            if (!modal || !modalBody) return;

            // Field isimlerini normalize et
            const productName = product.name || product.Name || 'İsimsiz Ürün';
            const productDescription = product.description || product.Description;
            const productPrice = product.price || product.Price || 0;
            const productImages = product.images || product.Images || [];
            const hasCampaign = product.hasCampaign || product.HasCampaign;
            const campaignCaption = product.campaignCaption || product.CampaignCaption;
            const campaignDetail = product.campaignDetail || product.CampaignDetail;
            const hasKufePoints = product.hasKufePoints || product.HasKufePoints;
            const kufePoints = product.kufePoints || product.KufePoints || 0;

            // Modal content oluştur
            const modalHTML = `
            <div class="product-modal-content">
                ${productImages && productImages.length > 0 ? `
                    <div class="product-gallery">
                        <div class="gallery-main">
                            <img src="${productImages[0].Original || productImages[0].original}" alt="${productName}" class="main-image" id="mainProductImage">
                            ${productImages.length > 1 ? `
                                <div class="gallery-nav">
                                    <button class="gallery-btn prev" onclick="QRMenuApp.previousImage()">
                                        <i class="fas fa-chevron-left"></i>
                                    </button>
                                    <button class="gallery-btn next" onclick="QRMenuApp.nextImage()">
                                        <i class="fas fa-chevron-right"></i>
                                    </button>
                                </div>
                            ` : ''}
                        </div>
                        ${productImages.length > 1 ? `
                            <div class="gallery-thumbs">
                                ${productImages.map((img, index) => `
                                    <img src="${img.Thumbnail || img.thumbnail}" alt="${productName}" class="thumb ${index === 0 ? 'active' : ''}" 
                                         onclick="QRMenuApp.selectImage(${index})" data-index="${index}">
                                `).join('')}
                            </div>
                        ` : ''}
                    </div>
                ` : `
                    <div class="product-placeholder">
                        <i class="fas fa-image" style="font-size: 4rem; color: var(--accent);"></i>
                    </div>
                `}
                
                <div class="product-details">
                    <div class="product-header">
                        <h2 class="product-title">${productName}</h2>
                        <div class="product-badges-modal">
                            ${hasCampaign ? `<span class="badge campaign">${campaignCaption}</span>` : ''}
                            ${hasKufePoints && kufePoints > 0 ? `<span class="badge points">+${kufePoints} Puan</span>` : ''}
                        </div>
                    </div>
                    
                    ${productDescription ? `<p class="product-description-full">${productDescription}</p>` : ''}
                    
                    ${hasCampaign && campaignDetail ? `
                        <div class="campaign-detail">
                            <h4><i class="fas fa-gift"></i> Kampanya Detayı</h4>
                            <p>${campaignDetail}</p>
                        </div>
                    ` : ''}
                    
                    <div class="product-price-section">
                        <div class="price-main">₺${Number(productPrice).toFixed(2)}</div>
                        ${hasKufePoints && kufePoints > 0 ?
                    `<div class="points-info">
                                <i class="fas fa-star"></i> 
                                Bu üründen <strong>+${kufePoints} puan</strong> kazanırsınız
                            </div>` :
                    ''
                }
                    </div>
                </div>
            </div>
        `;

            modalBody.innerHTML = modalHTML;

            // Modal'ı göster
            modal.style.display = 'flex';
            document.body.style.overflow = 'hidden';

            // Current image index'i sakla
            this.currentImageIndex = 0;
            this.currentProductImages = productImages || [];

        } catch (error) {
            console.error('❌ Product modal error:', error);
        }
    }

    // 🖼️ IMAGE GALLERY CONTROLS
    selectImage(index) {
        const mainImage = document.getElementById('mainProductImage');
        const thumbs = document.querySelectorAll('.gallery-thumbs .thumb');

        if (mainImage && this.currentProductImages[index]) {
            mainImage.src = this.currentProductImages[index].Original || this.currentProductImages[index].original;
            this.currentImageIndex = index;

            // Thumb active state
            thumbs.forEach((thumb, i) => {
                thumb.classList.toggle('active', i === index);
            });
        }
    }

    nextImage() {
        if (this.currentProductImages.length > 1) {
            this.currentImageIndex = (this.currentImageIndex + 1) % this.currentProductImages.length;
            this.selectImage(this.currentImageIndex);
        }
    }

    previousImage() {
        if (this.currentProductImages.length > 1) {
            this.currentImageIndex = this.currentImageIndex === 0 ?
                this.currentProductImages.length - 1 :
                this.currentImageIndex - 1;
            this.selectImage(this.currentImageIndex);
        }
    }

    // 📱 TOUCH EVENTS
    initializeTouchEvents() {
        // Swipe to go back in products view
        const productsView = document.getElementById('productsView');
        if (productsView) {
            productsView.addEventListener('touchstart', this.handleTouchStart.bind(this), { passive: true });
            productsView.addEventListener('touchmove', this.handleTouchMove.bind(this), { passive: true });
        }
    }

    initializeModalTouchEvents() {
        const modal = document.getElementById('productModal');
        if (modal) {
            modal.addEventListener('touchstart', this.handleModalTouchStart.bind(this), { passive: true });
            modal.addEventListener('touchmove', this.handleModalTouchMove.bind(this), { passive: true });
        }
    }

    handleTouchStart(e) {
        this.touchStartX = e.touches[0].clientX;
        this.touchStartY = e.touches[0].clientY;
    }

    handleTouchMove(e) {
        if (!this.touchStartX || !this.touchStartY) return;

        const touchEndX = e.touches[0].clientX;
        const touchEndY = e.touches[0].clientY;

        const diffX = this.touchStartX - touchEndX;
        const diffY = this.touchStartY - touchEndY;

        // Horizontal swipe is more significant
        if (Math.abs(diffX) > Math.abs(diffY) && Math.abs(diffX) > 50) {
            if (diffX < 0 && this.currentView === 'products') {
                // Swipe right - go back
                this.showCategories();
            }
        }

        this.touchStartX = 0;
        this.touchStartY = 0;
    }

    handleModalTouchStart(e) {
        this.modalTouchStartX = e.touches[0].clientX;
    }

    handleModalTouchMove(e) {
        if (!this.modalTouchStartX) return;

        const touchEndX = e.touches[0].clientX;
        const diffX = this.modalTouchStartX - touchEndX;

        if (Math.abs(diffX) > 50) {
            if (diffX > 0) {
                this.nextImage();
            } else {
                this.previousImage();
            }
        }

        this.modalTouchStartX = 0;
    }

    // 🔄 VIEW MANAGEMENT
    showProductsView() {
        document.getElementById('categoriesView').style.display = 'none';
        document.getElementById('productsView').style.display = 'block';
        this.currentView = 'products';

        // Scroll to top
        window.scrollTo({ top: 0, behavior: 'smooth' });
    }

    showCategories() {
        document.getElementById('productsView').style.display = 'none';
        document.getElementById('categoriesView').style.display = 'block';
        this.currentView = 'categories';
        this.currentCategory = null;

        // Scroll to top
        window.scrollTo({ top: 0, behavior: 'smooth' });
    }

    // 🎯 SMART SUGGESTION
    prepareSuggestion(suggestion) {
        console.log('🎯 Preparing suggestion:', suggestion);

        // Field isimlerini normalize et (backend küçük harf gönderiyor)
        if (!suggestion || !suggestion.product) {
            console.log('❌ No suggestion or product data');
            return;
        }

        this.pendingSuggestion = {
            icon: suggestion.icon || suggestion.Icon || '🌤️',
            message: suggestion.message || suggestion.Message || '',
            product: suggestion.product || suggestion.Product
        };

        console.log('✅ Suggestion prepared:', this.pendingSuggestion);
    }

    scheduleSmartSuggestion() {
        if (!this.pendingSuggestion) return;

        // 10 saniye sonra göster
        setTimeout(() => {
            if (this.currentView === 'categories' && this.pendingSuggestion) {
                this.showSmartSuggestion();
            }
        }, 10000);
    }

    showSmartSuggestion() {
        console.log('🎯 Showing suggestion:', this.pendingSuggestion);

        if (!this.pendingSuggestion) {
            console.log('❌ No pending suggestion');
            return;
        }

        const popup = document.getElementById('smartSuggestion');
        const icon = document.getElementById('suggestionIcon');
        const message = document.getElementById('suggestionMessage');
        const productDiv = document.getElementById('suggestedProduct');

        if (!popup) {
            console.log('❌ Suggestion popup element not found');
            return;
        }

        const suggestion = this.pendingSuggestion;

        if (icon) icon.textContent = suggestion.icon;
        if (message) message.textContent = suggestion.message;

        if (productDiv && suggestion.product) {
            const product = suggestion.product;

            productDiv.innerHTML = `
            <div class="suggested-product-card" onclick="QRMenuApp.showProductModal('${product.id}'); QRMenuApp.closeSuggestion();">
                ${product.image ?
                    `<img src="${product.image}" alt="${product.name}">` :
                    `<div class="product-icon">🍽️</div>`
                }
                <div class="suggestion-product-info">
                    <h4>${product.name}</h4>
                    <p class="suggestion-price">₺${Number(product.price).toFixed(2)}</p>
                    ${product.hasKufePoints && product.kufePoints > 0 ?
                    `<span class="suggestion-points">+${product.kufePoints} puan</span>` :
                    ''
                }
                </div>
            </div>
        `;
        }

        console.log('✅ Showing popup');
        popup.style.display = 'flex';

        // 15 saniye sonra otomatik kapat
        setTimeout(() => {
            this.closeSuggestion();
        }, 15000);
    }

    closeSuggestion() {
        const popup = document.getElementById('smartSuggestion');
        if (popup) {
            popup.style.display = 'none';
        }
        this.pendingSuggestion = null;
    }

    // 👤 USER PANEL
    toggleUserPanel() {
        const panel = document.getElementById('userPanel');
        if (!panel) return;

        const isVisible = panel.style.display === 'flex';

        if (isVisible) {
            panel.style.display = 'none';
            document.body.style.overflow = '';
        } else {
            this.loadUserPanel();
            panel.style.display = 'flex';
            document.body.style.overflow = 'hidden';
        }
    }

    // 👤 USER PANEL - ADVANCED VERSION
    loadUserPanel() {
        const panelBody = document.getElementById('userPanelBody');
        if (!panelBody) return;

        if (this.customerData) {
            // ✅ Giriş yapmış kullanıcı - TAB SİSTEMİ
            panelBody.innerHTML = `
            <!-- Kullanıcı Bilgileri -->
            <div class="panel-section">
                <div class="user-info">
                    <div class="user-avatar">
                        <i class="fas fa-user"></i>
                    </div>
                    <h4>${this.customerData.name}</h4>
                    <p>${this.customerData.phone}</p>
                    ${this.customerData.lastLogin ?
                    `<small>Son giriş: ${new Date(this.customerData.lastLogin).toLocaleDateString('tr-TR')}</small>` :
                    ''
                }
                </div>
                <div class="points-display">
                    <div class="points-value">${this.customerData.points.toLocaleString()}</div>
                    <div class="points-label">Küfe Point</div>
                </div>
            </div>

            <!-- Tab Menu -->
            <div class="panel-section">
                <div class="settings-tabs">
                    <button class="tab-button active" onclick="QRMenuApp.showTab('history')">
                        <i class="fas fa-history"></i> Geçmiş
                    </button>
                    <button class="tab-button" onclick="QRMenuApp.showTab('settings')">
                        <i class="fas fa-cog"></i> Ayarlar
                    </button>
                </div>

                <!-- Puan Geçmişi Tab -->
                <div id="historyTab" class="tab-content active">
                    <h6 class="section-title">
                        <i class="fas fa-star"></i> Puan Geçmişi
                    </h6>
                    <div id="pointHistory" class="point-history">
                        <div style="text-align: center; padding: var(--space-lg); color: var(--text-secondary);">
                            <i class="spinner"></i> Yükleniyor...
                        </div>
                    </div>
                </div>

                <!-- Ayarlar Tab -->
                <div id="settingsTab" class="tab-content">
                    <h6 class="section-title">
                        <i class="fas fa-key"></i> Şifre Değiştir
                    </h6>
                    <form id="changePasswordForm" onsubmit="QRMenuApp.changePassword(event)">
                        <div class="form-group">
                            <label>Mevcut Şifre</label>
                            <input type="password" name="currentPassword" class="form-control" required minlength="4">
                        </div>
                        <div class="form-group">
                            <label>Yeni Şifre</label>
                            <input type="password" name="newPassword" class="form-control" required minlength="4">
                        </div>
                        <div class="form-group">
                            <label>Yeni Şifre (Tekrar)</label>
                            <input type="password" name="confirmPassword" class="form-control" required minlength="4">
                        </div>
                        <button type="submit" class="btn-primary">
                            <i class="fas fa-save"></i> Şifreyi Değiştir
                        </button>
                    </form>
                </div>
            </div>

            <!-- Çıkış -->
            <div class="panel-section">
                <button class="btn-logout" onclick="QRMenuApp.logout()">
                    <i class="fas fa-sign-out-alt"></i> Güvenli Çıkış
                </button>
            </div>
        `;

            // Puan geçmişini yükle
            this.loadPointHistory();
        } else {
            // ✅ Giriş yapmamış kullanıcı - ŞİFRELİ GİRİŞ
            panelBody.innerHTML = `
            <div class="login-form">
                <h4>Küfe Point Hesabınız</h4>
                <p>Hesabınıza giriş yapın</p>
                
                <form id="loginForm" onsubmit="QRMenuApp.loginCustomer(event)">
                    <div class="form-group">
                        <label>
                            <i class="fas fa-phone"></i> Telefon Numarası
                        </label>
                        <input 
                            type="tel" 
                            name="phoneNumber"
                            class="form-control" 
                            placeholder="05xxxxxxxxx" 
                            maxlength="11"
                            required
                            oninput="this.value = this.value.replace(/[^0-9]/g, '')"
                        >
                    </div>
                    
                    <div class="form-group">
                        <label>
                            <i class="fas fa-lock"></i> Şifre
                        </label>
                        <input 
                            type="password" 
                            name="password"
                            class="form-control" 
                            placeholder="Şifrenizi girin"
                            required
                            minlength="4"
                        >
                    </div>
                    
                    <button type="submit" class="btn-primary">
                        <i class="fas fa-sign-in-alt"></i> Giriş Yap
                    </button>
                </form>
                
                <div class="login-info">
                    <i class="fas fa-info-circle"></i>
                    <div>
                        <strong>Henüz üye değil misiniz?</strong><br>
                        Üye olmak için garsonunuzdan yardım alın. Küfe point ile hesap öderken indirimlerden yararlanın.
                    </div>
                </div>
            </div>
        `;
        }
    }

    // 🔐 YENİ: Şifreli Giriş
    async loginCustomer(event) {
        event.preventDefault();

        const form = event.target;
        const formData = new FormData(form);
        const phoneNumber = formData.get('phoneNumber').trim();
        const password = formData.get('password').trim();

        if (!phoneNumber || !password) {
            this.showError('Lütfen tüm alanları doldurun.');
            return;
        }

        // Loading state
        const submitButton = form.querySelector('button[type="submit"]');
        const originalHTML = submitButton.innerHTML;
        submitButton.innerHTML = '<i class="spinner"></i> Giriş yapılıyor...';
        submitButton.disabled = true;

        try {
            const response = await fetch('/api/qr-menu/customer/login', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({
                    phoneNumber: phoneNumber,
                    password: password
                })
            });

            const result = await response.json();

            if (result.success) {
                this.customerData = result.customer;
                this.loadUserPanel();
                this.showSuccess(result.message);
            } else {
                this.showError(result.message);
            }

        } catch (error) {
            console.error('❌ Login error:', error);
            this.showError('Giriş sırasında hata oluştu. Lütfen tekrar deneyin.');
        } finally {
            // Reset button
            submitButton.innerHTML = originalHTML;
            submitButton.disabled = false;
        }
    }

    // 📊 YENİ: Puan Geçmişi Yükleme
    async loadPointHistory() {
        if (!this.customerData) return;

        const historyContainer = document.getElementById('pointHistory');
        if (!historyContainer) return;

        try {
            const response = await fetch(`/api/qr-menu/customer/point-history/${this.customerData.id}`);
            const result = await response.json();

            if (result.success && result.transactions.length > 0) {
                const historyHTML = result.transactions.map(transaction => {
                    const isEarned = transaction.type === 'Earned';
                    const date = new Date(transaction.date).toLocaleDateString('tr-TR', {
                        day: '2-digit',
                        month: '2-digit',
                        year: 'numeric',
                        hour: '2-digit',
                        minute: '2-digit'
                    });

                    return `
                    <div class="history-item">
                        <div class="history-info">
                            <div class="history-description">${transaction.description}</div>
                            <div class="history-date">${date}</div>
                        </div>
                        <div class="history-points ${isEarned ? 'earned' : 'spent'}">
                            ${isEarned ? '+' : '-'}${Math.abs(transaction.points)}
                        </div>
                    </div>
                `;
                }).join('');

                historyContainer.innerHTML = historyHTML;
            } else {
                historyContainer.innerHTML = `
                <div style="text-align: center; padding: var(--space-xl); color: var(--text-secondary);">
                    <i class="fas fa-history" style="font-size: 2rem; margin-bottom: var(--space-md); opacity: 0.5;"></i>
                    <p>Henüz puan işleminiz bulunmuyor.</p>
                </div>
            `;
            }

        } catch (error) {
            console.error('❌ Point history error:', error);
            historyContainer.innerHTML = `
            <div style="text-align: center; padding: var(--space-xl); color: var(--danger);">
                <i class="fas fa-exclamation-triangle"></i>
                <p>Puan geçmişi yüklenirken hata oluştu.</p>
            </div>
        `;
        }
    }

    // 🔧 YENİ: Tab Değiştirme
    showTab(tabName) {
        // Tab buttons
        document.querySelectorAll('.tab-button').forEach(btn => {
            btn.classList.remove('active');
        });

        // Tab contents
        document.querySelectorAll('.tab-content').forEach(content => {
            content.classList.remove('active');
        });

        // Activate selected
        event.target.classList.add('active');
        document.getElementById(tabName + 'Tab').classList.add('active');
    }

    // 🔑 YENİ: Şifre Değiştirme
    async changePassword(event) {
        event.preventDefault();

        const form = event.target;
        const formData = new FormData(form);
        const currentPassword = formData.get('currentPassword').trim();
        const newPassword = formData.get('newPassword').trim();
        const confirmPassword = formData.get('confirmPassword').trim();

        // Validasyon
        if (!currentPassword || !newPassword || !confirmPassword) {
            this.showError('Lütfen tüm alanları doldurun.');
            return;
        }

        if (newPassword !== confirmPassword) {
            this.showError('Yeni şifreler eşleşmiyor!');
            return;
        }

        if (newPassword.length < 4) {
            this.showError('Şifre en az 4 karakter olmalıdır!');
            return;
        }

        // Loading state
        const submitButton = form.querySelector('button[type="submit"]');
        const originalHTML = submitButton.innerHTML;
        submitButton.innerHTML = '<i class="spinner"></i> Değiştiriliyor...';
        submitButton.disabled = true;

        try {
            const response = await fetch('/api/qr-menu/customer/change-password', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({
                    customerId: this.customerData.id,
                    currentPassword: currentPassword,
                    newPassword: newPassword
                })
            });

            const result = await response.json();

            if (result.success) {
                this.showSuccess(result.message);
                form.reset(); // Formu temizle
            } else {
                this.showError(result.message);
            }

        } catch (error) {
            console.error('❌ Change password error:', error);
            this.showError('Şifre değiştirme sırasında hata oluştu.');
        } finally {
            // Reset button
            submitButton.innerHTML = originalHTML;
            submitButton.disabled = false;
        }
    }

    // 🚪 Logout Güncelleme
    logout() {
        this.customerData = null;
        this.loadUserPanel();
        this.showSuccess('Güvenle çıkış yapıldı. 👋');
    }


    // 🔍 CHECK CUSTOMER - IMPROVED VERSION
    async checkCustomer() {
        const phoneInput = document.getElementById('phoneInput');
        if (!phoneInput || !phoneInput.value.trim()) {
            this.showError('Lütfen telefon numaranızı girin.');
            return;
        }

        const phoneNumber = phoneInput.value.trim();

        // ✅ Basit telefon numarası validasyonu
        if (phoneNumber.length < 10 || phoneNumber.length > 11) {
            this.showError('Lütfen geçerli bir telefon numarası girin.');
            return;
        }

        // Loading state
        const button = phoneInput.nextElementSibling;
        const originalHTML = button.innerHTML;
        button.innerHTML = '<i class="fas fa-spinner fa-spin"></i>';
        button.disabled = true;

        try {
            const response = await fetch('/api/qr-menu/customer/quick-check', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ phoneNumber: phoneNumber })
            });

            const result = await response.json();

            if (result.success && result.isRegistered) {
                this.customerData = result.customer;
                this.loadUserPanel();
                this.showSuccess(`Merhaba ${result.customer.name}! 👋`);
            } else {
                this.showError(result.message || 'Bu telefon numarasına kayıtlı üyelik bulunamadı.');
            }

        } catch (error) {
            console.error('❌ Customer check error:', error);
            this.showError('Kontrol sırasında hata oluştu. Lütfen tekrar deneyin.');
        } finally {
            // Reset button
            button.innerHTML = originalHTML;
            button.disabled = false;
        }
    }



    // 🔄 MODAL MANAGEMENT
    closeProductModal() {
        const modal = document.getElementById('productModal');
        if (modal) {
            modal.style.display = 'none';
            document.body.style.overflow = '';
        }
    }

    closeAllModals() {
        this.closeProductModal();
        this.closeSuggestion();

        const userPanel = document.getElementById('userPanel');
        if (userPanel && userPanel.style.display === 'flex') {
            this.toggleUserPanel();
        }
    }

    // 🔄 LOADER MANAGEMENT
    async hideLoader() {
        // Tüm yüklemeler tamamlandı mı kontrol et
        const allLoaded = Object.values(this.loadingStates).every(state => state);

        if (!allLoaded) {
            console.log('⏳ Waiting for all components...');
            return;
        }

        const loader = document.getElementById('globalLoader');
        const app = document.getElementById('qrMenuApp');

        if (loader && app) {
            // Smooth transition
            loader.style.opacity = '0';

            setTimeout(() => {
                loader.style.display = 'none';
                app.style.display = 'block';
                app.classList.add('loaded');
            }, 500);
        }
    }

    // 🚨 ERROR HANDLING
    showError(message) {
        this.showToast(message, 'error');
    }

    showSuccess(message) {
        this.showToast(message, 'success');
    }

    showToast(message, type = 'info') {
        // Mevcut toast'ları temizle
        document.querySelectorAll('.toast').forEach(toast => toast.remove());

        const toast = document.createElement('div');
        toast.className = `toast toast-${type}`;

        const icon = type === 'success' ? 'fa-check-circle' :
            type === 'error' ? 'fa-exclamation-triangle' :
                'fa-info-circle';

        toast.innerHTML = `
        <i class="fas ${icon}"></i>
        <span>${message}</span>
    `;

        document.body.appendChild(toast);

        // 3 saniye sonra kaldır
        setTimeout(() => {
            if (toast.parentNode) {
                toast.style.opacity = '0';
                setTimeout(() => {
                    if (toast.parentNode) {
                        toast.remove();
                    }
                }, 300);
            }
        }, 3000);
    }
}

// 🚀 GLOBAL INSTANCE & API
let QRMenuApp;

document.addEventListener('DOMContentLoaded', () => {
    QRMenuApp = new QRMenuEngine();
});

window.QRMenuApp = {
    selectCategory: (category) => QRMenuApp?.selectCategory(category),
    showCategories: () => QRMenuApp?.showCategories(),
    showProductModal: (id) => QRMenuApp?.showProductModal(id),
    closeProductModal: () => QRMenuApp?.closeProductModal(),
    toggleUserPanel: () => QRMenuApp?.toggleUserPanel(),
    closeSuggestion: () => QRMenuApp?.closeSuggestion(),
    loginCustomer: (event) => QRMenuApp?.loginCustomer(event), // ✅ YENİ
    logout: () => QRMenuApp?.logout(),
    selectImage: (index) => QRMenuApp?.selectImage(index),
    nextImage: () => QRMenuApp?.nextImage(),
    previousImage: () => QRMenuApp?.previousImage(),
    showTab: (tabName) => QRMenuApp?.showTab(tabName), // ✅ YENİ
    changePassword: (event) => QRMenuApp?.changePassword(event), // ✅ YENİ
};