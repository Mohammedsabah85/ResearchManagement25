// CreateUser.js - منفصل عن Razor لتجنب مشاكل التنسيق
class CreateUserForm {
    constructor() {
        this.form = document.getElementById('createUserForm');
        this.initializeEventListeners();
        this.initializeValidation();
    }

    initializeEventListeners() {
        // Password functionality
        this.bindEvent('generatePassword', 'click', () => this.generatePassword());
        this.bindEvent('togglePassword', 'click', () => this.togglePasswordVisibility());

        // Form actions
        this.bindEvent('clearForm', 'click', () => this.clearForm());
        this.bindEvent('previewBtn', 'click', () => this.showPreview());
        this.bindEvent('confirmCreate', 'click', () => this.submitForm());

        // Real-time validation
        this.bindEvent('Email', 'blur', (e) => this.validateEmail(e.target.value));
        this.bindEvent('Password', 'input', (e) => this.updatePasswordStrength(e.target.value));

        // Role-based visibility
        this.bindEvent('Role', 'change', (e) => this.handleRoleChange(e.target.value));

        // Form submission
        if (this.form) {
            this.form.addEventListener('submit', (e) => this.handleFormSubmit(e));
        }
    }

    bindEvent(elementId, eventType, handler) {
        const element = document.getElementById(elementId);
        if (element) {
            element.addEventListener(eventType, handler);
        }
    }

    initializeValidation() {
        const requiredFields = ['FirstName', 'LastName', 'Email', 'Password', 'ConfirmPassword', 'Role'];

        requiredFields.forEach(fieldName => {
            const field = document.getElementById(fieldName);
            if (field) {
                field.addEventListener('blur', () => this.validateRequiredField(field));
                field.addEventListener('input', () => this.clearFieldError(field));
            }
        });

        // Password confirmation validation
        this.bindEvent('ConfirmPassword', 'input', () => this.validatePasswordConfirmation());
    }

    // Password Management
    generatePassword(length = 12) {
        const charset = {
            lowercase: 'abcdefghijklmnopqrstuvwxyz',
            uppercase: 'ABCDEFGHIJKLMNOPQRSTUVWXYZ',
            numbers: '0123456789',
            symbols: '!@#$%^&*'
        };

        const allChars = Object.values(charset).join('');
        let password = '';

        // Ensure at least one character from each category
        Object.values(charset).forEach(chars => {
            password += chars[Math.floor(Math.random() * chars.length)];
        });

        // Fill remaining length
        for (let i = 4; i < length; i++) {
            password += allChars[Math.floor(Math.random() * allChars.length)];
        }

        // Shuffle password
        password = password.split('').sort(() => Math.random() - 0.5).join('');

        // Set password fields
        const passwordField = document.getElementById('Password');
        const confirmField = document.getElementById('ConfirmPassword');

        if (passwordField && confirmField) {
            passwordField.value = password;
            confirmField.value = password;
            this.updatePasswordStrength(password);
            this.showNotification('تم إنشاء كلمة مرور قوية', 'success');
        }

        return password;
    }

    togglePasswordVisibility() {
        const passwordField = document.getElementById('Password');
        const confirmField = document.getElementById('ConfirmPassword');
        const toggleBtn = document.getElementById('togglePassword');
        const icon = toggleBtn?.querySelector('i');

        if (passwordField && confirmField && icon) {
            const isPassword = passwordField.type === 'password';

            passwordField.type = isPassword ? 'text' : 'password';
            confirmField.type = isPassword ? 'text' : 'password';

            icon.className = isPassword ? 'fas fa-eye-slash' : 'fas fa-eye';
        }
    }

    updatePasswordStrength(password) {
        const strengthIndicator = document.getElementById('passwordStrength');
        if (!strengthIndicator) return;

        const strength = this.calculatePasswordStrength(password);
        const strengthClasses = ['weak', 'fair', 'good', 'strong'];
        const strengthTexts = ['ضعيفة', 'متوسطة', 'جيدة', 'قوية'];

        strengthIndicator.className = `password-strength ${strengthClasses[strength.level]}`;
        strengthIndicator.innerHTML = `
            <div class="strength-bar">
                <div class="strength-fill" style="width: ${(strength.level + 1) * 25}%"></div>
            </div>
            <small>قوة كلمة المرور: ${strengthTexts[strength.level]}</small>
        `;
    }

    calculatePasswordStrength(password) {
        let score = 0;
        const checks = [
            password.length >= 8,
            /[a-z]/.test(password),
            /[A-Z]/.test(password),
            /[0-9]/.test(password),
            /[^A-Za-z0-9]/.test(password)
        ];

        score = checks.filter(check => check).length;

        return {
            level: Math.min(3, Math.max(0, score - 1)),
            score: score
        };
    }

    // Validation
    validateRequiredField(field) {
        const value = field.value.trim();
        if (!value) {
            this.showFieldError(field, 'هذا الحقل مطلوب');
            return false;
        }
        this.clearFieldError(field);
        return true;
    }

    validateEmail(email) {
        const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
        const emailField = document.getElementById('Email');

        if (email && !emailRegex.test(email)) {
            this.showFieldError(emailField, 'تنسيق البريد الإلكتروني غير صحيح');
            return false;
        }

        if (email) {
            this.checkEmailAvailability(email);
        }

        return true;
    }

    validatePasswordConfirmation() {
        const password = document.getElementById('Password')?.value;
        const confirmPassword = document.getElementById('ConfirmPassword')?.value;
        const confirmField = document.getElementById('ConfirmPassword');

        if (password && confirmPassword && password !== confirmPassword) {
            this.showFieldError(confirmField, 'كلمة المرور وتأكيد كلمة المرور غير متطابقين');
            return false;
        }

        if (confirmPassword) {
            this.clearFieldError(confirmField);
        }
        return true;
    }

    validateForm() {
        let isValid = true;
        this.clearAllErrors();

        // Validate required fields
        const requiredFields = ['FirstName', 'LastName', 'Email', 'Password', 'ConfirmPassword', 'Role'];

        requiredFields.forEach(fieldName => {
            const field = document.getElementById(fieldName);
            if (field && !this.validateRequiredField(field)) {
                isValid = false;
            }
        });

        // Validate email format
        const emailField = document.getElementById('Email');
        if (emailField && !this.validateEmail(emailField.value)) {
            isValid = false;
        }

        // Validate password confirmation
        if (!this.validatePasswordConfirmation()) {
            isValid = false;
        }

        // Validate password strength
        const password = document.getElementById('Password')?.value;
        if (password && password.length < 6) {
            this.showFieldError(document.getElementById('Password'), 'كلمة المرور يجب أن تحتوي على 6 أحرف على الأقل');
            isValid = false;
        }

        return isValid;
    }

    // Error Handling
    showFieldError(field, message) {
        if (!field) return;

        field.classList.add('is-invalid');
        const errorElement = field.parentElement.querySelector('.text-danger') ||
            field.nextElementSibling;
        if (errorElement && errorElement.classList.contains('text-danger')) {
            errorElement.textContent = message;
        }
    }

    clearFieldError(field) {
        if (!field) return;

        field.classList.remove('is-invalid');
        const errorElement = field.parentElement.querySelector('.text-danger') ||
            field.nextElementSibling;
        if (errorElement && errorElement.classList.contains('text-danger')) {
            errorElement.textContent = '';
        }
    }

    clearAllErrors() {
        document.querySelectorAll('.is-invalid').forEach(field => {
            field.classList.remove('is-invalid');
        });
        document.querySelectorAll('.text-danger').forEach(error => {
            error.textContent = '';
        });
    }

    // Form Actions
    clearForm() {
        if (confirm('هل أنت متأكد من مسح جميع البيانات المدخلة؟')) {
            this.form.reset();
            this.clearAllErrors();
            const passwordStrength = document.getElementById('passwordStrength');
            if (passwordStrength) {
                passwordStrength.innerHTML = '';
            }
        }
    }

    handleFormSubmit(e) {
        e.preventDefault();

        if (this.validateForm()) {
            this.showPreview();
        } else {
            this.showNotification('يرجى تصحيح الأخطاء المطلوبة', 'error');
            this.scrollToFirstError();
        }
    }

    scrollToFirstError() {
        const firstError = document.querySelector('.is-invalid');
        if (firstError) {
            firstError.scrollIntoView({ behavior: 'smooth', block: 'center' });
            firstError.focus();
        }
    }

    // Preview and Submission
    showPreview() {
        const formData = this.collectFormData();
        const previewHtml = this.generatePreviewHtml(formData);

        const previewContent = document.getElementById('previewContent');
        if (previewContent) {
            previewContent.innerHTML = previewHtml;
        }

        // Show modal using Bootstrap
        const previewModal = document.getElementById('previewModal');
        if (previewModal && window.bootstrap) {
            const modal = new bootstrap.Modal(previewModal);
            modal.show();
        }
    }

    collectFormData() {
        const getValue = (id) => {
            const element = document.getElementById(id);
            return element ? element.value : '';
        };

        const getChecked = (id) => {
            const element = document.getElementById(id);
            return element ? element.checked : false;
        };

        const getRoleText = () => {
            const roleSelect = document.getElementById('Role');
            const selectedOption = roleSelect?.querySelector('option:checked');
            return selectedOption ? selectedOption.textContent : '';
        };

        return {
            firstName: getValue('FirstName'),
            lastName: getValue('LastName'),
            firstNameEn: getValue('FirstNameEn'),
            lastNameEn: getValue('LastNameEn'),
            email: getValue('Email'),
            role: getRoleText(),
            institution: getValue('Institution'),
            academicDegree: getValue('AcademicDegree'),
            orcidId: getValue('OrcidId'),
            isActive: getChecked('IsActive'),
            emailConfirmed: getChecked('EmailConfirmed')
        };
    }

    generatePreviewHtml(data) {
        return `
            <div class="row">
                <div class="col-md-6">
                    <h6 class="text-primary">المعلومات الشخصية</h6>
                    <p><strong>الاسم:</strong> ${data.firstName} ${data.lastName}</p>
                    ${data.firstNameEn ? `<p><strong>الاسم (إنجليزي):</strong> ${data.firstNameEn} ${data.lastNameEn}</p>` : ''}
                    <p><strong>البريد الإلكتروني:</strong> ${data.email}</p>
                    <p><strong>الدور:</strong> ${data.role}</p>
                </div>
                <div class="col-md-6">
                    <h6 class="text-primary">المعلومات الأكاديمية</h6>
                    ${data.institution ? `<p><strong>المؤسسة:</strong> ${data.institution}</p>` : '<p class="text-muted">لم يتم تحديد مؤسسة</p>'}
                    ${data.academicDegree ? `<p><strong>الدرجة العلمية:</strong> ${data.academicDegree}</p>` : '<p class="text-muted">لم يتم تحديد درجة علمية</p>'}
                    ${data.orcidId ? `<p><strong>معرف ORCID:</strong> ${data.orcidId}</p>` : '<p class="text-muted">لا يوجد معرف ORCID</p>'}
                </div>
                <div class="col-12 mt-3">
                    <h6 class="text-primary">إعدادات الحساب</h6>
                    <div class="row">
                        <div class="col-6">
                            <p><strong>تفعيل الحساب:</strong> 
                                <span class="badge ${data.isActive ? 'bg-success' : 'bg-secondary'}">${data.isActive ? 'مفعل' : 'غير مفعل'}</span>
                            </p>
                        </div>
                        <div class="col-6">
                            <p><strong>تأكيد البريد الإلكتروني:</strong> 
                                <span class="badge ${data.emailConfirmed ? 'bg-success' : 'bg-warning'}">${data.emailConfirmed ? 'مؤكد' : 'غير مؤكد'}</span>
                            </p>
                        </div>
                    </div>
                </div>
            </div>
        `;
    }

    submitForm() {
        const previewModal = document.getElementById('previewModal');
        if (previewModal && window.bootstrap) {
            const modal = bootstrap.Modal.getInstance(previewModal);
            if (modal) {
                modal.hide();
            }
        }

        // Show loading state
        const submitBtn = document.getElementById('submitBtn');
        if (submitBtn) {
            const originalText = submitBtn.innerHTML;
            submitBtn.innerHTML = '<i class="fas fa-spinner fa-spin me-2"></i>جار الإنشاء...';
            submitBtn.disabled = true;
        }

        // Submit form
        if (this.form) {
            this.form.submit();
        }
    }

    // Utility Methods
    async checkEmailAvailability(email) {
        try {
            const response = await fetch(`/api/users/check-email?email=${encodeURIComponent(email)}`);
            const result = await response.json();

            if (!result.available) {
                this.showFieldError(document.getElementById('Email'), 'البريد الإلكتروني مستخدم بالفعل');
            }
        } catch (error) {
            console.warn('Failed to check email availability:', error);
        }
    }

    handleRoleChange(role) {
        // Handle role-specific field visibility
        const academicFields = document.querySelectorAll('[data-academic-field]');
        const showAcademicFields = ['researcher', 'professor', 'student'].includes(role.toLowerCase());

        academicFields.forEach(field => {
            field.style.display = showAcademicFields ? 'block' : 'none';
        });
    }

    showNotification(message, type = 'info') {
        // Create and show notification
        const notification = document.createElement('div');
        const alertClass = type === 'error' ? 'danger' : type;
        notification.className = `alert alert-${alertClass} alert-dismissible fade show position-fixed`;
        notification.style.cssText = 'top: 20px; right: 20px; z-index: 9999; min-width: 300px;';
        notification.innerHTML = `
            ${message}
            <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
        `;

        document.body.appendChild(notification);

        // Auto remove after 5 seconds
        setTimeout(() => {
            if (notification.parentNode) {
                notification.remove();
            }
        }, 5000);
    }
}

// Form Enhancements Utility Class
class FormEnhancements {
    static addLoadingState(button, loadingText = 'جار المعالجة...') {
        if (!button) return () => { };

        const originalText = button.innerHTML;
        button.innerHTML = `<i class="fas fa-spinner fa-spin me-2"></i>${loadingText}`;
        button.disabled = true;

        return () => {
            button.innerHTML = originalText;
            button.disabled = false;
        };
    }

    static addTooltips() {
        // Initialize Bootstrap tooltips if available
        if (window.bootstrap && window.bootstrap.Tooltip) {
            const tooltipElements = document.querySelectorAll('[title]');
            tooltipElements.forEach(element => {
                new bootstrap.Tooltip(element);
            });
        }
    }

    static addKeyboardShortcuts(form) {
        document.addEventListener('keydown', (e) => {
            // Ctrl+S to save
            if (e.ctrlKey && e.key === 's') {
                e.preventDefault();
                if (form) {
                    form.dispatchEvent(new Event('submit'));
                }
            }

            // Ctrl+R to reset
            if (e.ctrlKey && e.key === 'r') {
                e.preventDefault();
                const clearBtn = document.getElementById('clearForm');
                if (clearBtn) clearBtn.click();
            }

            // Escape to cancel
            if (e.key === 'Escape') {
                const cancelBtn = document.querySelector('a[asp-action="Index"]');
                if (cancelBtn && !document.querySelector('.modal.show')) {
                    if (confirm('هل تريد إلغاء العملية والعودة للقائمة؟')) {
                        window.location.href = cancelBtn.href;
                    }
                }
            }
        });
    }
}

// Initialize when DOM is ready
document.addEventListener('DOMContentLoaded', function () {
    const createUserForm = new CreateUserForm();

    // Add enhancements
    FormEnhancements.addTooltips();
    FormEnhancements.addKeyboardShortcuts(createUserForm.form);

    console.log('Create User Form initialized successfully');
});