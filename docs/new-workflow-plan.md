# خطة تحويل النظام — نظام البريد الداخلي

## 📌 الهدف
1. تحويل النظام من "نظام إدارة العمليات" إلى **نظام البريد الداخلي** (نظام الاستلام والتسليم).
2. تبسيط workflow النظام إلى **شخصيتين فقط** بدلاً من النموذج الحالي (المرسل + الشؤون + المستلم).
3. إضافة ميزة **نسخة (CC)** تُشبه البريد الإلكتروني — إشعارات + قراءة فقط.
4. **إشعار المدير تلقائياً** عند الإرسال لموظف لديه مباشرة.

---

## 📛 التسمية الجديدة

### اسم النظام واختصاره
- **الاسم**: نظام البريد الداخلي — Internal Mail System
- **الاختصار**: **HBMail** (بدلاً من HBOperations)
- **الرقم المرجعي**: `HBM-YYYYMM-XXXXX` (بدلاً من `HB-YYYYMM-XXXXX`)

### جدول استبدال المصطلحات (UI)

| المصطلح القديم | المصطلح الجديد | ملاحظات |
|---------------|---------------|---------|
| معاملة | بريد | المصطلح الأساسي |
| المعاملات | البريد / صندوق البريد | في القوائم والعناوين |
| إنشاء معاملة جديدة | إرسال بريد جديد | شاشة الإنشاء |
| قائمة المعاملات | صندوق البريد | الصفحة الرئيسية |
| تفاصيل المعاملة | تفاصيل البريد | شاشة التفاصيل |
| رقم المعاملة / الرقم المرجعي | رقم البريد | `HBM-YYYYMM-XXXXX` |
| نوع المعاملة | نوع البريد | MailType |
| حالة المعاملة | حالة البريد | MailStatus |
| أولوية المعاملة | أولوية البريد | MailPriority |
| أرشيف المعاملات | أرشيف البريد | صفحة الأرشيف |
| تقارير المعاملات | تقارير البريد | صفحة التقارير |
| سجل التدقيق | سجل التدقيق | (لا يتغير) |
| المستندات | المرفقات | أنسب لسياق البريد |
| تأكيد الاستلام | تأكيد الاستلام | (لا يتغير) |
| توجيه لموظف | توجيه لموظف | (جديد) |
| رفض | رفض / إرجاع | أو "إعادة البريد" |

---

### إعادة تسمية المشروع (Project / Namespace)

| القديم | الجديد |
|--------|--------|
| `HBOperations.slnx` | `HBMail.slnx` |
| `HBOperations.Domain/` | `HBMail.Domain/` |
| `HBOperations.Application/` | `HBMail.Application/` |
| `HBOperations.Infrastructure/` | `HBMail.Infrastructure/` |
| `HBOperations.Web/` | `HBMail.Web/` |
| namespace `HBOperations.*` | namespace `HBMail.*` |
| folder `HBOperations/` (root) | `HBMail/` |

**خطوات تسمية المشروع:**
1. إعادة تسمية مجلدات المشاريع
2. إعادة تسمية ملفات `.csproj`
3. تحديث Solution file
4. Find & Replace لكل `HBOperations` → `HBMail` في جميع الملفات (namespaces, using, project references)
5. حذف `bin/` و `obj/` وإعادة البناء

---

### إعادة تسمية الكيانات (Entities)

| الكيان القديم | الكيان الجديد | الملف |
|-------------|-------------|-------|
| `Transaction` | `Mail` | `Transaction.cs` → `Mail.cs` |
| `TransactionDocument` | `MailAttachment` | `TransactionDocument.cs` → `MailAttachment.cs` |
| `TransactionHistory` | `MailHistory` | `TransactionHistory.cs` → `MailHistory.cs` |
| `TransactionNote` | `MailNote` | `TransactionNote.cs` → `MailNote.cs` |
| `MailCC` | `MailCC` | (جديد) |
| `AuditLog` | `AuditLog` | (لا يتغير) |
| `Branch` | `Branch` | (لا يتغير) |
| `Department` | `Department` | (لا يتغير) |
| `Notification` | `Notification` | (لا يتغير) |
| `SystemSetting` | `SystemSetting` | (لا يتغير) |

---

### إعادة تسمية الـ Enums

| القديم | الجديد |
|--------|--------|
| `TransactionStatus` | `MailStatus` |
| `TransactionType` | `MailType` |
| `TransactionPriority` | `MailPriority` |
| `NotificationType.TransactionCreated` | `NotificationType.MailCreated` |
| `NotificationType.TransactionStatusChanged` | `NotificationType.MailStatusChanged` |
| `NotificationType.TransactionAssigned` | `NotificationType.MailAssigned` |
| `NotificationType.TransactionOverdue` | `NotificationType.MailOverdue` |
| `DocumentType` | `AttachmentType` |
| `BranchType` | `BranchType` (لا يتغير) |

---

### إعادة تسمية الـ DTOs

| القديم | الجديد |
|--------|--------|
| `TransactionSummaryDto` | `MailSummaryDto` |
| `TransactionDetailDto` | `MailDetailDto` |
| `DocumentDto` | `AttachmentDto` |
| `DashboardStatsDto` | `DashboardStatsDto` (لا يتغير) |
| `BranchDto` | `BranchDto` (لا يتغير) |
| باقي report DTOs | لا تتغير |

---

### إعادة تسمية الـ StateMachine

| القديم | الجديد | الملف |
|--------|--------|-------|
| `TransactionStateMachine` | `MailStateMachine` | `TransactionStateMachine.cs` → `MailStateMachine.cs` |

---

### إعادة تسمية EF Configurations

| القديم | الجديد |
|--------|--------|
| `TransactionConfiguration` | `MailConfiguration` |
| `TransactionDocumentConfiguration` | `MailAttachmentConfiguration` |
| `TransactionHistoryConfiguration` | `MailHistoryConfiguration` |
| `TransactionNoteConfiguration` | `MailNoteConfiguration` |
| `MailCCConfiguration` | (جديد) |

---

### إعادة تسمية DbSets في AppDbContext

| القديم | الجديد |
|--------|--------|
| `DbSet<Transaction> Transactions` | `DbSet<Mail> Mails` |
| `DbSet<TransactionDocument> TransactionDocuments` | `DbSet<MailAttachment> MailAttachments` |
| `DbSet<TransactionHistory> TransactionHistories` | `DbSet<MailHistory> MailHistories` |
| `DbSet<TransactionNote> TransactionNotes` | `DbSet<MailNote> MailNotes` |
| — | `DbSet<MailCC> MailCCs` (جديد) |

> ⚠️ أسماء الجداول في SQL ستتغير تلقائياً مع Migration.

---

### إعادة تسمية صفحات Blazor

| القديم | الجديد | Route |
|--------|--------|-------|
| `Transactions/CreateTransaction.razor` | `Mail/ComposeMail.razor` | `/mail/compose` |
| `Transactions/TransactionDetail.razor` | `Mail/MailDetail.razor` | `/mail/{id}` |
| `Transactions/TransactionList.razor` | `Mail/Inbox.razor` | `/mail` |
| `Transactions/EditTransaction.razor` | `Mail/EditMail.razor` | `/mail/edit/{id}` |
| `Archive.razor` | `Archive.razor` | (لا يتغير) |
| `Reports/Reports.razor` | `Reports/Reports.razor` | (لا يتغير) |
| `Home.razor` | `Home.razor` | (لا يتغير) |

---

### إعادة تسمية الخدمات

| القديم | الجديد | ملاحظات |
|--------|--------|---------|
| `AutoArchiveService` | `AutoArchiveService` | لا يتغير — عام |
| `PdfReportService` | `PdfReportService` | لا يتغير — عام |
| `ExcelHelpers` | `ExcelHelpers` | لا يتغير — عام |
| `NotificationService` | `NotificationService` | لا يتغير |
| `NotificationEventService` | `NotificationEventService` | لا يتغير |
| `AuditService` | `AuditService` | لا يتغير |
| باقي الخدمات | لا تتغير | (FileStorage, Email, etc.) |

> ✅ الخدمات أسماؤها عامة ولا تحتوي على كلمة "Transaction" — لا تحتاج تغيير.

---

### تنظيف البيانات

> البيانات الحالية **بيانات اختبار فقط** — تُحذف بالكامل ما عدا:
> - ✅ **يبقى**: المستخدمون (`AspNetUsers`), الأدوار (`AspNetRoles`), الفروع (`Branches`), الإدارات (`Departments`), إعدادات النظام (`SystemSettings`)
> - ❌ **يُحذف**: المعاملات (`Transactions`), المستندات (`TransactionDocuments`), السجل (`TransactionHistories`), الملاحظات (`TransactionNotes`), الإشعارات (`Notifications`), سجل التدقيق (`AuditLogs`)
>
> **الطريقة**: حذف وإعادة إنشاء DB بعد Migration، أو SQL script لتفريغ الجداول المحددة.

---

## 🔍 الفهم الحالي للنظام (Workflow القديم)

### الحالات (TransactionStatus)
```
Sent      = 0   (مُرسل — بانتظار الشؤون)
InTransit = 1   (قيد التوصيل — الشؤون استلمت)
Received  = 2   (مُستلم — المستلم أكّد)
Rejected  = 3   (مرفوض)
Archived  = 4   (مؤرشف)
```

### الانتقالات
```
Sent ──► InTransit (PickUp بواسطة الشؤون الإدارية DEP-ADM)
       └► Rejected (الشؤون ترفض)
InTransit ──► Received (المستلم يؤكد)
            └► Rejected (المستلم يرفض)
Received/Rejected ──► Archived (SuperAdmin/ITAdmin أو AutoArchive)
```

### الأدوار (الحالية — لن تتغير)
- `SuperAdmin`, `CEO`, `AssistantCEO`, `ITAdmin`
- `DepartmentManager`, `BranchManager`, `OfficeManager`
- `DepartmentStaff`, `BranchStaff`
- `Auditor`, `ComplianceOfficer`, `ShariahAuditor`

### نقاط الحقن في الكيان `Transaction`
- `SenderUserId`, `SenderBranchId`, `SenderDepartmentId` — المرسل
- `ReceiverBranchId`, `ReceiverDepartmentId`, `ReceiverUserId` — المستلم
- `PickedUpByUserId`, `PickedUpAt`, `IsSelfDelivery`, `CourierName` — الشؤون (ستُحذف)
- `ReceivedByUserId`, `ReceivedAt`, `ReceiverNote` — تأكيد الاستلام
- `RejectionNote` — سبب الرفض

---

## 🎯 Workflow الجديد (المطلوب)

### الفلسفة
- **المرسل دائماً = الشؤون الإدارية** (DEP-ADM) فقط — هي من تُنشئ البريد وترسله.
- **المستلم له نوعان**:

#### النوع 1: مدير (Manager)
- يُوجَّه له البريد باسمه (ReceiverUserId = ID المدير).
- المدير لديه **خياران**:
  1. **يستلم بنفسه** → الحالة تصبح `Received`.
  2. **يوجّه الاستلام لموظف في إدارته** → تتغير `ReceiverUserId` إلى الموظف، وتنتقل الحالة إلى `AssignedToStaff`.
- الموظف الموجَّه إليه يستلم → `Received`.

#### النوع 2: موظف (Staff)
- الشؤون تختار اسم الموظف مباشرة.
- الموظف يفتح البريد ويستلم → `Received` مباشرة.
- **مدير الموظف يحصل تلقائياً على إشعار** بأنه تم إرسال بريد لموظفه، مع إمكانية تصفح تفاصيل البريد **للقراءة فقط**.

### ميزة النسخة (CC) — جديدة
- عند إنشاء البريد، يمكن للشؤون **إضافة مستخدمين في حقل "نسخة إلى" (CC)**.
- هؤلاء المستخدمون:
  - ✅ يحصلون على **إشعار عند الإرسال** (بريد جديد أُرسل ومذكورين في النسخة).
  - ✅ يحصلون على **إشعار عند تأكيد الاستلام** (تم استلام البريد).
  - ✅ يستطيعون **تصفح تفاصيل البريد للقراءة فقط** (لا يمكنهم تعديل/استلام/رفض).
  - ❌ لا يظهر لهم أزرار الإجراءات (استلام/رفض/توجيه).
- يُخزَّن في كيان مستقل `MailCC` (many-to-many بين Mail و User).

### إشعار المدير التلقائي (عند الإرسال لموظف مباشرة)
- عندما تُرسل الشؤون بريداً **لموظف مباشرة** (ليس لمدير):
  1. يُحدَّد مدير الموظف تلقائياً (نفس DepartmentId أو BranchId + دور Manager).
  2. يُرسل **إشعار تلقائي للمدير**: "تم إرسال بريد لموظفك [اسم الموظف]".
  3. المدير يستطيع **فتح تفاصيل البريد للقراءة فقط** (مثل CC تماماً).
- الفرق عن CC: هذا الإشعار **تلقائي** — لا يحتاج أن تضيفه الشؤون يدوياً.

### حالات Workflow الجديد
```
Sent              = 0   (الشؤون أنشأت البريد وأرسلته)
AssignedToStaff   = 1   (مدير وجّه الاستلام لموظف لديه)
Received          = 2   (المستلم النهائي أكّد الاستلام)
Rejected          = 3   (المستلم — مدير أو موظف — رفض/أرجع البريد)
Archived          = 4   (مؤرشف)
```

> ✅ **استبدال** `InTransit` بـ `AssignedToStaff` — نفس القيمة `1` لتجنب كسر البيانات.

### خريطة الانتقالات الجديدة
```
Sent ──► Received          (المستلم يستلم بنفسه — مدير أو موظف)
     ──► AssignedToStaff   (المدير يوجّه لموظف لديه)
     ──► Rejected          (المستلم يرفض/يُرجع البريد)

AssignedToStaff ──► Received   (الموظف الموجَّه يستلم)
                ──► Rejected   (الموظف يرفض)

Received/Rejected ──► Archived
```

### مصفوفة الإشعارات الجديدة

| الحدث | مَن يُشعَر | نوع الإشعار |
|-------|-----------|-------------|
| إرسال بريد لمدير | المدير (المستلم) + CC | بريد جديد |
| إرسال بريد لموظف مباشرة | الموظف (المستلم) + مديره (تلقائي) + CC | بريد جديد + إشعار تلقائي للمدير |
| توجيه من مدير لموظف | الموظف الجديد + الشؤون (المرسل) | تم توجيه البريد |
| تأكيد الاستلام | الشؤون + المدير (إن وجَّه أو أُشعر تلقائياً) + CC | تم الاستلام |
| رفض/إرجاع | الشؤون + المدير (إن وجَّه أو أُشعر تلقائياً) + CC | تم الرفض |
| أرشفة | — | لا إشعار |

---

## 📝 خطة التنفيذ

### المرحلة 0: إعادة تسمية المشروع والكيانات (HBOperations → HBMail)
**هذه المرحلة تُنفَّذ أولاً قبل أي تغيير آخر.**

**الخطوات:**
1. **إعادة تسمية ملفات المشروع والمجلدات:**
   - `src/HBOperations.Domain/` → `src/HBMail.Domain/`
   - `src/HBOperations.Application/` → `src/HBMail.Application/`
   - `src/HBOperations.Infrastructure/` → `src/HBMail.Infrastructure/`
   - `src/HBOperations.Web/` → `src/HBMail.Web/`
   - `HBOperations.slnx` → `HBMail.slnx`
   - إعادة تسمية ملفات `.csproj` داخل كل مشروع

2. **Find & Replace شامل**: `HBOperations` → `HBMail` في:
   - جميع ملفات `.cs` (namespaces, using statements)
   - جميع ملفات `.razor` و `.razor.cs`
   - ملفات `.csproj` (project references)
   - `_Imports.razor`
   - `appsettings.json` / `appsettings.Development.json`
   - `Program.cs`
   - `launchSettings.json`

3. **إعادة تسمية الكيانات (Entity Classes):**
   - `Transaction` → `Mail` (+ كل الخصائص التي تُشير إليه)
   - `TransactionDocument` → `MailAttachment`
   - `TransactionHistory` → `MailHistory`
   - `TransactionNote` → `MailNote`
   - تسمية الملفات: `Transaction.cs` → `Mail.cs`, etc.

4. **إعادة تسمية الـ Enums:**
   - `TransactionStatus` → `MailStatus`
   - `TransactionType` → `MailType`
   - `TransactionPriority` → `MailPriority`
   - `DocumentType` → `AttachmentType`
   - قيم `NotificationType`: `TransactionCreated` → `MailCreated`, etc.

5. **إعادة تسمية الـ DTOs:**
   - `TransactionSummaryDto` → `MailSummaryDto`
   - `TransactionDetailDto` → `MailDetailDto`
   - `DocumentDto` → `AttachmentDto`

6. **إعادة تسمية الـ StateMachine:**
   - `TransactionStateMachine` → `MailStateMachine`
   - الملف: `TransactionStateMachine.cs` → `MailStateMachine.cs`

7. **إعادة تسمية EF Configurations:**
   - `TransactionConfiguration` → `MailConfiguration`
   - `TransactionDocumentConfiguration` → `MailAttachmentConfiguration`
   - `TransactionHistoryConfiguration` → `MailHistoryConfiguration`
   - `TransactionNoteConfiguration` → `MailNoteConfiguration`

8. **إعادة تسمية DbSets:**
   - `Transactions` → `Mails`
   - `TransactionDocuments` → `MailAttachments`
   - `TransactionHistories` → `MailHistories`
   - `TransactionNotes` → `MailNotes`

9. **إعادة تسمية صفحات Blazor:**
   - مجلد `Transactions/` → `Mail/`
   - `CreateTransaction.razor` → `ComposeMail.razor` (route: `/mail/compose`)
   - `TransactionDetail.razor` → `MailDetail.razor` (route: `/mail/{id}`)
   - `TransactionList.razor` → `Inbox.razor` (route: `/mail`)
   - `EditTransaction.razor` → `EditMail.razor` (route: `/mail/edit/{id}`)

10. **تحديث الرقم المرجعي:**
    - `HB-YYYYMM-XXXXX` → `HBM-YYYYMM-XXXXX`

11. **حذف `bin/` و `obj/`** وإعادة البناء (build).

### المرحلة 1: كيان CC الجديد + تنظيف البيانات
**الملفات المتأثرة:**
- `src/HBMail.Domain/Entities/MailCC.cs` ← **ملف جديد**
- `src/HBMail.Infrastructure/Data/AppDbContext.cs`
- `src/HBMail.Infrastructure/Data/Configurations/` ← **إعداد جديد**

**التغييرات:**
1. إنشاء كيان `MailCC`:
   ```csharp
   public class MailCC : BaseEntity
   {
       public Guid MailId { get; set; }
       public Mail Mail { get; set; } = null!;
       public Guid UserId { get; set; }
       public DateTime AddedAt { get; set; }
   }
   ```
2. إضافة `DbSet<MailCC>` في `AppDbContext`.
3. إضافة navigation property في `Mail`:
   ```csharp
   public ICollection<MailCC> CcRecipients { get; set; } = new List<MailCC>();
   ```
4. إنشاء `MailCCConfiguration` (Composite index على MailId + UserId).
5. **تنظيف البيانات**: حذف بيانات الاختبار من الجداول:
   - ❌ حذف: `Mails`, `MailAttachments`, `MailHistories`, `MailNotes`, `Notifications`, `AuditLogs`
   - ✅ يبقى: `AspNetUsers`, `AspNetRoles`, `Branches`, `Departments`, `SystemSettings`

### المرحلة 1: تبسيط Enums والكيان
**الملفات المتأثرة:**
- `src/HBMail.Domain/Enums/Enums.cs`
- `src/HBMail.Domain/Entities/Mail.cs`

**التغييرات:**
1. إعادة تسمية `InTransit` إلى `AssignedToStaff` (نفس القيمة 1) — ضمن `MailStatus` enum.
2. إزالة الحقول غير المستخدمة من `Mail` (تمت في المرحلة 0).
3. إضافة الحقول الجديدة (تمت في المرحلة 0).

> هذه المرحلة تعتني بالقيم والمنطق داخل الـ Enums — إعادة التسمية الهيكلية تمت في المرحلة 0.

### المرحلة 3: إعادة كتابة `MailStateMachine`
**الملف:** `src/HBMail.Application/Workflow/MailStateMachine.cs`

**التغييرات:**
1. حذف `PickUp` (لم يعد له معنى).
2. إضافة `AssignToStaff(tx, managerId, staffUserId, note)`.
3. تحديث `ConfirmReceipt` ليعمل من `Sent` أو `AssignedToStaff`.
4. تحديث `Reject` ليعمل من `Sent` أو `AssignedToStaff`.
5. تحديث `GetContextualTransitions`:
   - `Sent` + المستخدم = ReceiverUserId ومدير → [Received, AssignedToStaff, Rejected]
   - `Sent` + المستخدم = ReceiverUserId وموظف → [Received, Rejected]
   - `AssignedToStaff` + المستخدم = ReceiverUserId الجديد → [Received, Rejected]
   - المستخدم في CC أو ManagerNotifiedUserId → [] (لا إجراءات — قراءة فقط)
6. إضافة `CanViewReadOnly(tx, userId)` — يُرجع `true` إذا:
   - المستخدم في CC
   - أو المستخدم = `ManagerNotifiedUserId`
   - أو المستخدم = `OriginalReceiverUserId` (مدير سابق بعد التوجيه)

### المرحلة 4: شاشة إرسال بريد جديد
**الملف:** `src/HBMail.Web/Components/Pages/Mail/ComposeMail.razor`

**التغييرات:**
1. تغيير العنوان من "إنشاء معاملة جديدة" إلى "إرسال بريد جديد".
2. **Authorization**: الشؤون الإدارية (DEP-ADM) فقط.
3. **اختيار المستلم** — حقل واحد:
   - فلتر: [الكل] [مديرون] [موظفون]
   - قائمة: الاسم + الفرع/الإدارة + الدور (badge)
4. **حقل CC جديد** — "نسخة إلى":
   - Multi-select dropdown مع بحث.
   - يعرض المستخدمين (ما عدا المستلم الأساسي والمرسل).
   - يمكن إضافة عدة مستخدمين.
5. تحديث التسميات: "الموضوع"، "المرفقات"، "ملاحظة المرسل"، إلخ.
6. **منطق إشعار المدير التلقائي**: عند الإرسال لموظف:
   - البحث عن مدير في نفس الإدارة/الفرع.
   - حفظ `ManagerNotifiedUserId`.
   - إرسال إشعار تلقائي.

### المرحلة 5: شاشة تفاصيل البريد
**الملف:** `src/HBMail.Web/Components/Pages/Mail/MailDetail.razor`

**التغييرات:**
1. تغيير العنوان والتسميات ("تفاصيل البريد"، "المرفقات"، إلخ).
2. حذف زر "استلام من المرسل" (PickUp).
3. إضافة زر **"توجيه لموظف في إدارتي"** (للمديرين فقط عند الحالة `Sent`).
4. عرض قائمة CC (إن وُجدت) — أسماء المنسوخ إليهم.
5. **وضع القراءة فقط**:
   - إذا المستخدم في CC أو ManagerNotifiedUserId → يرى كل التفاصيل لكن **بدون أزرار إجراءات**.
   - رسالة صغيرة: "أنت مذكور في نسخة هذا البريد (للاطلاع فقط)".
6. تحديث Timeline (3 مراحل):
   - أُرسل → (وُجِّه لموظف؟) → استُلم
7. زر "تأكيد الاستلام" يعمل من `Sent` و `AssignedToStaff`.
8. زر "رفض / إرجاع" يعمل من `Sent` و `AssignedToStaff`.

### المرحلة 5: تحديث الصفحات الأخرى + التسميات
**الملفات المتأثرة:**
- `src/HBMail.Web/Components/Pages/Mail/Inbox.razor`
- `src/HBMail.Web/Components/Pages/Mail/EditMail.razor`
- `src/HBMail.Web/Components/Pages/Home.razor`
- `src/HBMail.Web/Components/Pages/Reports/Reports.razor`
- `src/HBMail.Web/Components/Pages/Archive.razor`
- `src/HBMail.Web/Components/Layout/HBHeader.razor`

**التغييرات:**
1. **استبدال كل التسميات** حسب جدول المصطلحات أعلاه:
   - "معاملة/معاملات" → "بريد"
   - "المعاملات" (في Nav) → "البريد" أو "صندوق البريد"
   - "إنشاء معاملة" (Quick Action) → "إرسال بريد جديد"
   - "مستندات" → "مرفقات"
2. استبدال `InTransit` بـ `AssignedToStaff` في الفلاتر والإحصائيات.
3. تحديث منطق نطاق الوصول:
   - الشؤون ترى كل البريد.
   - المدير يرى: بريده + بريد موظفيه + بريد وُجِّه لموظفيه مباشرة (ManagerNotifiedUserId) + CC.
   - الموظف يرى: بريده + CC.
   - CC users يرون البريد المنسوخ إليهم (قراءة فقط).

### المرحلة 6: الإشعارات
**الملفات:**
- `src/HBMail.Infrastructure/Services/NotificationService.cs`
- استخدامات الإشعارات في الصفحات و `Program.cs`

**التغييرات (حسب مصفوفة الإشعارات):**
1. **عند الإرسال**:
   - إشعار للمستلم (مدير أو موظف).
   - إذا المستلم موظف → إشعار تلقائي لمديره.
   - إشعار لكل مستخدمي CC.
2. **عند التوجيه** (AssignToStaff):
   - إشعار للموظف الجديد.
   - إشعار للشؤون (المرسل).
3. **عند الاستلام** (Received):
   - إشعار للشؤون + المدير (إن وجَّه `AssignedByUserId` أو أُشعر تلقائياً `ManagerNotifiedUserId`) + CC.
4. **عند الرفض/الإرجاع**:
   - إشعار للشؤون + المدير (إن وجَّه `AssignedByUserId` أو أُشعر تلقائياً `ManagerNotifiedUserId`) + CC.
5. حذف منطق إشعارات `PickUp`.

### المرحلة 7: AutoArchive + الأرشيف
**الملفات:**
- `src/HBMail.Web/Services/AutoArchiveService.cs`
- `src/HBMail.Web/Components/Pages/Archive.razor`

**التغييرات:**
- استبدال `InTransit` بـ `AssignedToStaff`.
- تحديث التسميات ("أرشيف البريد" بدلاً من "أرشيف المعاملات").

### المرحلة 8: التقارير و Excel/PDF
**الملفات:**
- `src/HBMail.Web/Services/PdfReportService.cs`
- `src/HBMail.Web/Services/ExcelHelpers.cs`
- `src/HBMail.Application/Common/DTOs/ReportDtos.cs`

**التغييرات:**
1. تحديث `GetStatusName(s)` → "موجَّه لموظف" بدلاً من "قيد التوصيل".
2. تحديث كل النصوص: "معاملة" → "بريد"، "مستندات" → "مرفقات".
3. تحديث عناوين ملفات Excel/PDF.

### المرحلة 9: Migration
**التغييرات:**
1. **حذف كل Migrations القديمة** وإنشاء Initial Migration جديدة (لأن البيانات اختبار وستُحذف).
2. إنشاء جدول `MailCCs`:
   - `Id`, `MailId` (FK), `UserId`, `AddedAt`
   - Composite unique index: `(MailId, UserId)`
3. الجداول المُعاد تسميتها ستُنشأ بالأسماء الجديدة تلقائياً:
   - `Mails`, `MailAttachments`, `MailHistories`, `MailNotes`, `MailCCs`
4. الأعمدة الجديدة في `Mails`:
   - `AssignedAt`, `AssignedByUserId`, `OriginalReceiverUserId`, `ManagerNotifiedUserId`
5. الأعمدة المحذوفة (لن تظهر في الـ entity أصلاً):
   - `PickedUpAt`, `PickedUpByUserId`, `IsSelfDelivery`, `CourierName`
6. Migration commands:
   ```
   # حذف DB القديمة وإعادة إنشائها
   dotnet ef database drop -f -p src/HBMail.Infrastructure -s src/HBMail.Web
   # حذف Migrations القديمة
   Remove-Item src/HBMail.Infrastructure/Data/Migrations/* -Recurse
   # إنشاء Migration جديدة
   dotnet ef migrations add InitialCreate -p src/HBMail.Infrastructure -s src/HBMail.Web
   dotnet ef database update -p src/HBMail.Infrastructure -s src/HBMail.Web
   ```
7. إعادة تشغيل Seed Data (المستخدمون + الأدوار + الفروع + الإدارات + الإعدادات).

### المرحلة 11: Build + Test
- بناء المشروع (0 errors).
- اختبار يدوي:
  1. إرسال بريد لمدير → المدير يستلم / يوجّه لموظف.
  2. إرسال بريد لموظف مباشرة → التحقق من إشعار المدير التلقائي.
  3. إضافة CC عند الإرسال → التحقق من الإشعارات + القراءة فقط.
  4. تأكيد الاستلام → إشعارات CC.
  5. رفض → إشعارات CC.
  6. التقارير + Excel + PDF.

---

## ⚠️ نقاط حرجة للانتباه

1. **تحديد مدير الموظف تلقائياً**: عند الإرسال لموظف مباشرة:
   - البحث عن مستخدم بنفس DepartmentId (أو BranchId) + دور Manager.
   - إذا لم يُوجد مدير → لا إشعار تلقائي (يمكن إضافته يدوياً في CC).

2. **CC vs إشعار المدير التلقائي**:
   - CC = اختيار يدوي من الشؤون عند الإنشاء.
   - إشعار المدير = تلقائي عند الإرسال لموظف.
   - كلاهما يمنح **قراءة فقط** — لكن CC مُخزَّن في `MailCC`، والمدير في `ManagerNotifiedUserId`.
   - في شاشة التفاصيل: نعاملهما بنفس الطريقة (عرض كامل، بدون أزرار).

3. **من يرى البريد في القوائم (نطاق الوصول)**:
   - المرسل (الشؤون) → كل البريد.
   - المستلم (مدير/موظف) → بريده.
   - CC users → البريد المنسوخ إليهم (مع علامة "نسخة").
   - مدير أُشعر تلقائياً → بريد موظفيه (مع علامة "إشعار تلقائي").
   - SuperAdmin/CEO → كل شيء.

4. **سلسلة المسؤولية**: إذا وجَّه المدير ثم رفض الموظف:
   - `OriginalReceiverUserId` = المدير الأصلي.
   - `AssignedByUserId` = المدير.
   - الرفض يُسجَّل باسم الموظف.
   - إشعار الرفض يصل للشؤون + المدير.

5. **الرجوع للوراء**: لا يمكن للمدير سحب التوجيه — للتعديل: رفض ثم إعادة إنشاء.

6. **`AdminNote`**: يبقى ويُعاد تسميته عرضياً إلى "ملاحظات المرسل عند الإرسال".

7. **`RequireReceiverDocument`**: يبقى كما هو — يُعرض كـ "مرفق إلزامي عند الاستلام".

8. **تحديد الموظفين عند التوجيه**: نفس الإدارة (DepartmentId) إذا موجودة، وإلا نفس الفرع (BranchId).

---

## 📊 ملخص الفروقات (قبل/بعد)

| الجانب | النظام القديم | النظام الجديد |
|--------|-------------|-------------|
| اسم النظام | نظام إدارة العمليات | نظام البريد الداخلي |
| المصطلح الأساسي | معاملة / Transaction | بريد / Mail |
| عدد الشخصيات | 3 (مرسل، شؤون، مستلم) | 2 (شؤون = مرسل، مستلم) |
| من ينشئ | أي موظف | الشؤون فقط |
| الحالات | Sent/InTransit/Received/Rejected/Archived | Sent/AssignedToStaff/Received/Rejected/Archived |
| اسم المشروع | HBOperations.* | HBMail.* |
| الكيانات | Transaction/TransactionDocument/... | Mail/MailAttachment/... |
| الرقم المرجعي | HB-YYYYMM-XXXXX | HBM-YYYYMM-XXXXX |
| خطوة الوسيط | الشؤون "تستلم وتسلِّم" | محذوفة |
| توجيه المدير لموظف | ❌ | ✅ |
| CC (نسخة) | ❌ | ✅ إشعار + قراءة فقط |
| إشعار المدير التلقائي | ❌ | ✅ عند الإرسال لموظف مباشرة |
| المستلم النهائي | فرع/إدارة/مستخدم | مستخدم فقط (مدير أو موظف) |

---

## 🚀 ترتيب التنفيذ

| # | المرحلة | الوصف |
|---|--------|-------|
| 0 | إعادة تسمية المشروع | HBOperations → HBMail (مجلدات + namespaces + csproj + solution) |
| 0b | إعادة تسمية الكيانات | Transaction → Mail, TransactionDocument → MailAttachment, etc. |
| 0c | إعادة تسمية Enums/DTOs | TransactionStatus → MailStatus, TransactionType → MailType, etc. |
| 0d | إعادة تسمية الصفحات | Transactions/ → Mail/, CreateTransaction → ComposeMail, etc. |
| 1 | كيان CC + تنظيف | إنشاء `MailCC` entity + حذف بيانات الاختبار |
| 2 | Enums + Entity | AssignedToStaff + حذف/إضافة حقول + ManagerNotifiedUserId |
| 3 | StateMachine | حذف PickUp + إضافة AssignToStaff + CanViewReadOnly |
| 4 | إرسال بريد | ComposeMail: CC picker + إشعار المدير التلقائي |
| 5 | تفاصيل البريد | MailDetail: وضع القراءة فقط + زر التوجيه + عرض CC |
| 6 | الصفحات الأخرى | Inbox + Home + Reports + Archive + Nav — تسميات + نطاق CC |
| 7 | الإشعارات | مصفوفة الإشعارات الجديدة (CC + مدير تلقائي) |
| 8 | AutoArchive + أرشيف | تسميات + AssignedToStaff |
| 9 | PDF + Excel | تسميات "بريد/مرفقات" + حالة "موجَّه لموظف" |
| 10 | Migration | حذف DB + Migrations قديمة → InitialCreate جديدة + Seed |
| 11 | Build + Test | بناء + اختبار يدوي شامل |

---

## ✅ القرارات المُعتمدة

| السؤال | القرار |
|--------|--------|
| اختصار النظام | **HBMail** |
| اسم النظام بالعربية | نظام البريد الداخلي |
| الرقم المرجعي | `HBM-YYYYMM-XXXXX` |
| تسمية الكيانات في الكود | `Transaction` → `Mail`, `TransactionDocument` → `MailAttachment`, etc. |
| تسمية المشروع/Namespace | `HBOperations.*` → `HBMail.*` |
| تسمية الصفحات | `Transactions/` → `Mail/`, `CreateTransaction` → `ComposeMail`, etc. |
| البيانات الحالية | حذف كل بيانات الاختبار — إبقاء Users/Roles/Branches/Departments/Settings |
| الأدوار الحالية | تبقى كما هي — لا نلمس RBAC |
| حقل AdminNote | يبقى — معناه عرضياً "ملاحظات المرسل عند الإرسال" |
| نطاق التوجيه | نفس الإدارة (DepartmentId)، وإلا نفس الفرع (BranchId) |
| RequireReceiverDocument | يبقى كما هو |
| إشعار المدير عند الإرسال لموظف | ✅ تلقائي + قراءة فقط |
| إشعار المدير عند تأكيد/رفض موظفه | ✅ (سواء وجَّه أو أُشعر تلقائياً) |
| ميزة CC | ✅ إشعار عند الإرسال + الاستلام + الرفض + قراءة فقط |
