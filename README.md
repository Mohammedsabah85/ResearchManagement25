# نظام إدارة البحوث العلمية - Research Management System

## 🎯 نظرة عامة

نظام شامل لإدارة البحوث العلمية مبني باستخدام ASP.NET Core مع Clean Architecture. يوفر النظام منصة متكاملة لتقديم ومراجعة ونشر البحوث العلمية.

## ✨ الميزات الرئيسية

### 📝 إدارة البحوث
- تقديم البحوث العلمية بواجهة سهلة الاستخدام
- دعم متعدد اللغات (عربي/إنجليزي)
- إدارة المؤلفين والملفات المرفقة
- تتبع حالة البحث عبر مراحل المراجعة

### 👥 إدارة المستخدمين
- نظام أدوار متقدم (باحث، مراجع، مدير مسار، أدمن)
- صلاحيات محكمة حسب الدور
- ملفات شخصية شاملة للمستخدمين

### 🔍 نظام المراجعة
- تعيين المراجعين تلقائياً حسب التخصص
- نماذج مراجعة مفصلة
- تقييم وتعليقات المراجعين
- تتبع تقدم المراجعة

### 📊 التقارير والإحصائيات
- لوحة تحكم تفاعلية
- إحصائيات شاملة للبحوث
- تقارير مفصلة للمراجعات
- تصدير البيانات بصيغ متعددة

### 🎨 واجهة المستخدم
- تصميم عصري ومتجاوب
- دعم الأجهزة المحمولة
- واجهة ثنائية اللغة
- تجربة مستخدم محسنة

## 🏗️ المعمارية التقنية

### Clean Architecture
```
├── Domain Layer (الطبقة الأساسية)
│   ├── Entities (الكيانات)
│   ├── Enums (التعدادات)
│   ├── Events (الأحداث)
│   └── Exceptions (الاستثناءات)
│
├── Application Layer (طبقة التطبيق)
│   ├── Commands (الأوامر)
│   ├── Queries (الاستعلامات)
│   ├── DTOs (نماذج النقل)
│   ├── Validators (المدققات)
│   └── Mappings (التحويلات)
│
├── Infrastructure Layer (طبقة البنية التحتية)
│   ├── Data (قاعدة البيانات)
│   ├── Repositories (المستودعات)
│   ├── Services (الخدمات)
│   └── External APIs (الواجهات الخارجية)
│
└── Web Layer (طبقة العرض)
    ├── Controllers (المتحكمات)
    ├── Views (العروض)
    ├── ViewModels (نماذج العرض)
    └── wwwroot (الملفات الثابتة)
```

### التقنيات المستخدمة
- **Backend:** ASP.NET Core 9.0
- **Database:** SQL Server + Entity Framework Core
- **Frontend:** HTML5, CSS3, JavaScript, Bootstrap 5
- **Architecture:** Clean Architecture + CQRS
- **Validation:** FluentValidation
- **Mapping:** AutoMapper
- **Logging:** Serilog
- **Authentication:** ASP.NET Core Identity

## 🚀 التثبيت والتشغيل

### المتطلبات
- .NET 9.0 SDK
- SQL Server 2019+
- Visual Studio 2022 أو VS Code

### خطوات التثبيت

1. **استنساخ المشروع**
```bash
git clone https://github.com/Mohamed-sabah/ResearchManagement25.git
cd ResearchManagement25
```

2. **تكوين قاعدة البيانات**
```bash
# تحديث connection string في appsettings.json
# تطبيق migrations
dotnet ef database update --project src/ResearchManagement.Infrastructure --startup-project src/ResearchManagement.Web
```

3. **تشغيل المشروع**
```bash
cd src/ResearchManagement.Web
dotnet run
```

4. **الوصول للتطبيق**
- افتح المتصفح على: `https://localhost:5001`
- المستخدم الافتراضي: admin@research.com
- كلمة المرور: Admin123!

## 📱 لقطات الشاشة

### لوحة التحكم
![Dashboard](docs/images/dashboard.png)

### قائمة البحوث
![Research List](docs/images/research-list.png)

### تفاصيل البحث
![Research Details](docs/images/research-details.png)

## 🔧 التكوين

### إعدادات قاعدة البيانات
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=.;Database=ResearchManagementDB;Trusted_Connection=true;TrustServerCertificate=true;"
  }
}
```

### إعدادات البريد الإلكتروني
```json
{
  "EmailSettings": {
    "SmtpServer": "smtp.gmail.com",
    "SmtpPort": 587,
    "SenderEmail": "your-email@gmail.com",
    "SenderPassword": "your-app-password",
    "EnableSsl": true
  }
}
```

### إعدادات رفع الملفات
```json
{
  "FileUploadSettings": {
    "MaxFileSize": 10485760,
    "AllowedExtensions": [".pdf", ".doc", ".docx", ".ppt", ".pptx"],
    "UploadPath": "wwwroot/uploads"
  }
}
```

## 👥 الأدوار والصلاحيات

### الباحث (Researcher)
- تقديم البحوث الجديدة
- تعديل البحوث المقدمة
- عرض حالة البحوث
- الرد على تعليقات المراجعين

### المراجع (Reviewer)
- مراجعة البحوث المعينة
- تقديم التقييمات والتعليقات
- تحديد قرار المراجعة
- عرض تاريخ المراجعات

### مدير المسار (Track Manager)
- إدارة البحوث في المسار
- تعيين المراجعين
- اتخاذ القرارات النهائية
- إدارة جدولة المراجعات

### المدير (Admin)
- إدارة جميع البحوث
- إدارة المستخدمين والأدوار
- عرض التقارير الشاملة
- تكوين النظام

## 🔄 سير العمل

### تقديم البحث
1. الباحث يقدم البحث مع الملفات
2. النظام يرسل إشعار تأكيد
3. مدير المسار يراجع البحث
4. تعيين المراجعين المناسبين

### عملية المراجعة
1. المراجع يستلم إشعار بالبحث
2. مراجعة البحث وتقديم التقييم
3. إرسال التعليقات للباحث
4. اتخاذ القرار النهائي

### النشر
1. قبول البحث من المراجعين
2. موافقة مدير المسار
3. نشر البحث في المجموعة
4. إشعار جميع الأطراف

## 🧪 الاختبار

### تشغيل الاختبارات
```bash
# Unit Tests
dotnet test tests/ResearchManagement.UnitTests

# Integration Tests
dotnet test tests/ResearchManagement.IntegrationTests
```

### تغطية الكود
```bash
dotnet test --collect:"XPlat Code Coverage"
```

## 📚 التوثيق

- [دليل المستخدم](docs/user-guide.md)
- [دليل المطور](docs/developer-guide.md)
- [API Documentation](docs/api-docs.md)
- [تقرير التحديثات](PROJECT_COMPLETION_REPORT.md)

## 🤝 المساهمة

نرحب بمساهماتكم! يرجى قراءة [دليل المساهمة](CONTRIBUTING.md) قبل البدء.

### خطوات المساهمة
1. Fork المشروع
2. إنشاء branch جديد (`git checkout -b feature/amazing-feature`)
3. Commit التغييرات (`git commit -m 'Add amazing feature'`)
4. Push للـ branch (`git push origin feature/amazing-feature`)
5. فتح Pull Request

## 📄 الترخيص

هذا المشروع مرخص تحت [MIT License](LICENSE).

## 📞 التواصل

- **المطور:** Mohamed Sabah
- **البريد الإلكتروني:** mohamed.sabah@example.com
- **GitHub:** [@Mohamed-sabah](https://github.com/Mohamed-sabah)

## 🙏 شكر وتقدير

شكر خاص لجميع المساهمين في تطوير هذا النظام:
- فريق التطوير
- المراجعين والمختبرين
- المجتمع المفتوح المصدر

---

**آخر تحديث:** يونيو 2025  
**الإصدار:** 1.0.0  
**الحالة:** جاهز للإنتاج ✅