class CustomerCreate {
    constructor() {
        this.form = document.getElementById('customerCreateForm');
        this.submitBtn = document.getElementById('submitBtn');
        this.searchBtn = document.getElementById('searchBtn');

        this.bindEvents();
        this.setupPhoneFormatting();
    }

    bindEvents() {
        this.form.addEventListener('submit', (e) => this.handleSubmit(e));

        document.getElementById('togglePassword')?.addEventListener('click', this.togglePassword);

        this.searchBtn.addEventListener('click', () => this.searchCustomer());

        document.getElementById('searchPhone').addEventListener('keypress', (e) => {
            if (e.key === 'Enter') {
                this.searchCustomer();
            }
        });

        const phoneInput = document.querySelector('input[name="phoneNumber"]');
        phoneInput.addEventListener('input', this.validatePhone);
    }

    setupPhoneFormatting() {
        const phoneInput = document.querySelector('input[name="phoneNumber"]');
        phoneInput.addEventListener('input', function (e) {
         
            this.value = this.value.replace(/[^0-9]/g, '');

            if (this.value.length > 11) {
                this.value = this.value.substring(0, 11);
            }
        });
    }

    validatePhone(e) {
        const phone = e.target.value;
        const phoneError = document.getElementById('phoneError');

        if (phone.length > 0 && phone.length < 11) {
            phoneError.textContent = 'Telefon numarası 11 haneli olmalıdır';
            phoneError.style.display = 'block';
        } else if (phone.length === 11 && !phone.startsWith('05')) {
            phoneError.textContent = 'Telefon numarası 05 ile başlamalıdır';
            phoneError.style.display = 'block';
        } else {
            phoneError.style.display = 'none';
        }
    }

    togglePassword() {
        const passwordInput = document.getElementById('passwordInput');
        const toggleIcon = document.querySelector('#togglePassword i');

        if (passwordInput.type === 'password') {
            passwordInput.type = 'text';
            toggleIcon.classList.replace('fa-eye', 'fa-eye-slash');
        } else {
            passwordInput.type = 'password';
            toggleIcon.classList.replace('fa-eye-slash', 'fa-eye');
        }
    }

    async handleSubmit(e) {
        e.preventDefault();

        if (!this.validateForm()) return;

        this.setLoading(true);

        try {
            const formData = new FormData(this.form);
            const customerData = {
                fullname: formData.get('fullname'),
                phoneNumber: formData.get('phoneNumber'),
                password: formData.get('password')
            };

            const response = await fetch('/Customer/Create', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify(customerData)
            });

            const result = await response.json();

            if (result.success) {
                this.showSuccess(result.message);
                this.resetForm();

                setTimeout(() => {
                    window.location.href = '/';
                }, 2000);
            } else {
                this.showError(result.message);
            }
        } catch (error) {
            this.showError('Bağlantı hatası oluştu!');
        } finally {
            this.setLoading(false);
        }
    }

    async searchCustomer() {
        const phone = document.getElementById('searchPhone').value.trim();
        const resultsDiv = document.getElementById('searchResults');

        if (!phone) {
            this.showError('Telefon numarası girin!');
            return;
        }

        if (phone.length < 3) {
            this.showError('En az 3 karakter girin!');
            return;
        }

        try {
            const response = await fetch(`/Customer/Search?phone=${encodeURIComponent(phone)}`);
            const result = await response.json();

            if (result.success) {
                this.displaySearchResults(result.customers);
            } else {
                resultsDiv.innerHTML = `
                    <div class="no-results">
                        <i class="fas fa-user-slash"></i>
                        <p>Müşteri bulunamadı</p>
                        <small>Bu telefon numarasında kayıtlı müşteri yok</small>
                    </div>
                `;
                resultsDiv.style.display = 'block';
            }
        } catch (error) {
            this.showError('Arama sırasında hata oluştu!');
        }
    }

    displaySearchResults(customers) {
        const resultsDiv = document.getElementById('searchResults');

        if (customers.length === 0) {
            resultsDiv.innerHTML = `
                <div class="no-results">
                    <i class="fas fa-user-slash"></i>
                    <p>Müşteri bulunamadı</p>
                </div>
            `;
        } else {
            resultsDiv.innerHTML = customers.map(customer => `
                <div class="customer-result">
                    <div class="customer-info">
                        <div class="customer-name">
                            <i class="fas fa-user"></i>
                            ${customer.name}
                        </div>
                        <div class="customer-phone">
                            <i class="fas fa-phone"></i>
                            ${customer.phone}
                        </div>
                    </div>
                    <div class="customer-status">
                        <span class="status-badge active">
                            <i class="fas fa-check-circle"></i>
                            Kayıtlı
                        </span>
                    </div>
                </div>
            `).join('');
        }

        resultsDiv.style.display = 'block';
    }

    validateForm() {
        const fullname = document.querySelector('input[name="fullname"]').value.trim();
        const phone = document.querySelector('input[name="phoneNumber"]').value.trim();
        const password = document.querySelector('input[name="password"]').value.trim();

        if (!fullname || fullname.length < 2) {
            this.showError('Ad soyad en az 2 karakter olmalıdır!');
            return false;
        }

        if (!phone || phone.length !== 11 || !phone.startsWith('05')) {
            this.showError('Geçerli bir telefon numarası girin! (05XXXXXXXXX)');
            return false;
        }

        if (!password || password.length < 4) {
            this.showError('Şifre en az 4 karakter olmalıdır!');
            return false;
        }

        return true;
    }

    setLoading(loading) {
        const btnText = this.submitBtn.querySelector('.btn-text');
        const btnLoading = this.submitBtn.querySelector('.btn-loading');

        if (loading) {
            btnText.style.display = 'none';
            btnLoading.style.display = 'flex';
            this.submitBtn.disabled = true;
        } else {
            btnText.style.display = 'flex';
            btnLoading.style.display = 'none';
            this.submitBtn.disabled = false;
        }
    }

    resetForm() {
        this.form.reset();
        document.querySelectorAll('.form-error').forEach(error => {
            error.style.display = 'none';
        });
    }

    showSuccess(message) {
        if (typeof window.showToast === 'function') {
            window.showToast(message, 'success');
        } else {
            alert('✅ ' + message);
        }
    }

    showError(message) {
        if (typeof window.showToast === 'function') {
            window.showToast(message, 'error');
        } else {
            alert('❌ ' + message);
        }
    }
}

document.addEventListener('DOMContentLoaded', () => {
    new CustomerCreate();
});