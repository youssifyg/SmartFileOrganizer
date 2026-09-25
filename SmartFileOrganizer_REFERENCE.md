# Smart File Organizer — AI Development Reference

> **Purpose:** Canonical, gate-based reference for AI coding sessions.
> **Rule:** One Gate/Phase at a time. STOP at the end of each Gate/Phase and wait for explicit approval.
> **Source:** User-provided SmartFileOrganizer roadmap.

---

## AI Session Protocol

Before editing code, the AI must identify the authorized Gate/Phase and obey only its scope.

### Required sequence
1. Read this reference and `Documentation/ARCHITECTURE.md`.
2. Confirm the current Gate/Phase.
3. Implement only the authorized scope.
4. Do not introduce future-phase features.
5. If an architectural change or additional interface is required, STOP and explain it before implementation.
6. Build and run the applicable tests.
7. Report the result.
8. STOP. Do not continue automatically.

### Absolute constraints
- Safety > aggressive cleanup.
- User control > automation.
- Content determines file identity.
- No permanent deletion by default.
- No direct UI access to Infrastructure or filesystem.
- No AI/LLM functionality inside the application.
- No cloud services.
- No unnecessary dependencies.

---

# Smart File Organizer — خطة التنفيذ الكاملة (Gate-Based Roadmap)

> استخدام هذا الملف: افتح جلسة جديدة مع الـ AI Assistant (Antigravity)، الصق قسم **Gate 0** أو **Phase الحالية** فقط + ملف `ARCHITECTURE.md`. ما تديهوش الـ PRD الكامل أبدًا. كل Phase = جلسة منفصلة، وبعد كل Gate: **STOP** وراجع النتيجة قبل ما تكمل.

---

## 0. الفلسفة الحاكمة (لا تتغير طول المشروع)

- `Discover → Explain → Let user decide → Clean safely`
- `Safety > Aggressive Cleanup`
- `User Control > Automation`
- File identity = المحتوى، مش الاسم ولا الموقع.
- لا حذف نهائي افتراضيًا — دايمًا Recycle Bin + تأكيد صريح (باستثناء تفريغ مسارات الـ Temp المحددة بعد تأكيد مغلّظ).
- **قاعدة أمان الأقراص (Drive Safety Guard):** حظر المسارات الحيوية لنظام التشغيل (`C:\Windows`, `C:\Program Files`, `Boot`, `System Volume Information`) من أي عمليات نقل أو حذف.
- ممنوع الـ AI ياخد قرار معماري لوحده. كل تغيير معماري = شرح + انتظار موافقة.

---

## 1. Architecture Foundation (تُحفظ في `/Documentation/ARCHITECTURE.md`)

### التقنية المختارة
`C# + .NET + WPF`

### هيكل الحلول

```text
SmartFileOrganizer/
├── src/
│   ├── SmartFileOrganizer.Core
│   ├── SmartFileOrganizer.Application
│   ├── SmartFileOrganizer.Infrastructure
│   └── SmartFileOrganizer.UI
└── tests/
    └── SmartFileOrganizer.Tests
```

| Project | Responsibility | Depends On |
|---|---|---|
| Core | Domain models + core interfaces | None |
| Application | Use cases + business logic | Core |
| Infrastructure | Filesystem / SQLite / Windows APIs | Core (+ Application عند الحاجة فقط) |
| UI | WPF only / MVVM | Application, Core |
| Tests | Unit + Integration | All relevant layers |

### قاعدة الاتجاه (Dependency Rule)

```text
Core
  ↑
Application
  ↑
UI

Infrastructure → Core
```

**ممنوع نهائيًا:** `UI → Infrastructure` مباشرة، `Core → Infrastructure`، `Core → UI`، `Application → UI`.

الـ Interfaces المسموح إنشاؤها في Gate 0 (تعريف فقط، بدون تنفيذ)

IFileScanner
IFileAnalyzer
IHashService
ISimilarityEngine
IFileOperationService
ITempCleanupService      // مسارات تفريغ %temp% و Windows Temp
IDriveRelocationService  // إدارة ونقل وتفريغ الملفات بين الأقراص (C: -> D:)
IRepository

```

أي Interface إضافي: الـ AI لازم يوقف ويشرح السبب قبل ما ينشئه.

### Domain Model المسموح في Gate 0

`FileRecord` — تمثيل مفاهيمي فقط. ممنوع بداخله: قراءة ملفات، حساب Hash، أي اعتماد على Windows APIs.

### قيود إضافية على Gate 0

- لا DI framework (لو محتاج تسجيل بسيط، أبسط طريقة built-in فقط).
- UI = نافذة Placeholder بس (بنية MVVM موجودة، مفيش Dashboard حقيقي).
- توثيق مختصر فقط داخل `ARCHITECTURE.md`.

### Definition of Done — Gate 0

- [ ] Restore ناجح
- [ ] Build ناجح
- [ ] WPF shell بيشتغل
- [ ] Tests بتعدي
- [ ] لا Circular Dependencies
- [ ] لا Packages زيادة عن الحاجة

STOP CONDITION — Gate 0
ممنوع البدء في: Scanner, Hashing, Database, Duplicate Detection, أي UI Feature, File Operations.

## 2. خارطة الطريق الكاملة (Phase 1 → Phase 11)

### Phase 1 — File Discovery Engine
| Sub-phase | المحتوى |
|---|---|
| 1A | Directory Enumeration (تصفح المجلدات فقط) |
| 1B | FileRecord Metadata (حجم، تواريخ، امتداد) |
| 1C | Filtering / Exclusions (استثناء مجلدات النظام تلقائيًا) |
| 1D | Progress + Cancellation (غير متزامن، لا يجمّد الـ UI) |
| 1E | Tests (ملفات كبيرة/فارغة/Unicode/عربي/مسارات طويلة) |

ممنوع فيها: أي Hashing، أي مقارنة تشابه، أي كتابة/حذف.
DoD: Scan لمجلد حقيقي بيرجع قائمة FileRecord صحيحة، Cancel شغّال، ملف واحد Inaccessible ما يوقفش السكان كله.

### Phase 2 — Hash Engine
الهدف: Partial Hash ثم Full SHA-256 بس للملفات المتساوية في الحجم.
النطاق: Group by Size → Partial Hash → Group by Partial Hash → Full Hash.
ممنوع فيها: أي Grouping نهائي لـ Duplicates، أي DB.
DoD: نفس المحتوى بأسماء مختلفة (بما فيها أسماء عربي) بيدّي نفس الـ Hash.

### Phase 3 — Duplicate Engine
الهدف: بناء DuplicateGroup من نتائج Phase 2 (نفس الحجم + نفس الـ Full Hash).
النطاق: حساب Recoverable Space لكل مجموعة.
ممنوع فيها: أي حذف، أي واجهة مستخدم حقيقية.
DoD: الـ 8 Test Cases الحرجة كلها ناجحة.

### Phase 4 — Local Database (SQLite)
الهدف: تخزين FileRecords, Scan Sessions, Hashes, DuplicateGroups.
النطاق: Repositories خلف IRepository، Migrations أساسية.
ممنوع فيها: أي Cloud sync، أي تخزين خارج الجهاز.
DoD: إعادة فتح التطبيق بتفضل النتائج موجودة بدون إعادة سكان كامل.

### Phase 5 — File Operations & System Storage Maintenance
تنقسم لمسارات محددة لضمان عزل المخاطر:
- **5A: Standard Operations**: Move to Recycle Bin (افتراضي)، Restore، Operation History. شاشة تأكيد إجبارية قبل أي عملية تعرض الحجم والعدد.
- **5B: Temp Cleanup Engine**: استهداف وتفريغ مسار المستخدم: %USERPROFILE%\AppData\Local\Temp (عبر %temp%). استهداف وتفريغ مسار النظام: C:\Windows\Temp. استراتيجية الأمان: تخطي فوري بدون إيقاف العملية (Safe Try-Catch-Skip).
- **5C: Drive Space Relocation Engine**: فحص توفر المساحة، التحقق من سلامة النقل: Copy → Verify Size/Hash → Delete Source File.

DoD: لا يتم أي حذف أو نقل دون شاشة مراجعة تعرض مسار المصدر والوجهة والمساحة المحررة.

### Phase 6 — UI & Dashboard
الهدف: Dashboard موحد مبني بنمط MVVM يربط العمليات:
- Scan & Duplicate Screen.
- Temp Cleaner Action.
- Drive Space Assistant.
- Settings Screen.
ممنوع فيها: الوصول المباشر لنظام الملفات من الـ UI بأي شكل.
DoD: تشغيل سيناريو كامل (فحص، كشف التكرارات، تنظيف Temp، ونقل ملفات بين الأقراص) من الواجهة مباشرة.

### Phase 7 — Similarity Framework (Extension Point فقط)
فتح نقطة امتداد ISimilarityEngine بدون تنفيذ محركات فعلية.

### Phase 14 — UI/UX Refinements & Polish (Current Phase)
الهدف: تحسين تجربة المستخدم، إصلاح أخطاء الواجهة الدقيقة، ودعم اللغات والمظاهر.
النطاق:
- **14A: Dashboard Fixes**: DataGrid Single-Click Checkbox, Real Progress Bar (IProgress integration).
- **14B: Settings Enhancements**: FolderBrowserDialog for Excluded Folders path selection.
- **14C: Localization**: الشمولية في التعريب (Arabic) ودعم RTL.
- **14D: Theming**: دعم Dark Mode وتناسق الخطوط والألوان.
DoD: جميع عناصر الواجهة تعمل بسلاسة بضغطة واحدة، شريط التقدم يعكس الواقع، الإعدادات تدعم التصفح، والتطبيق يدعم العربية والوضع الداكن بالكامل.

### Phases 8-11 (خارج نطاق الـ MVP)
Image, Video, Audio, Document Analyzers.

## 3. القواعد الذهبية للـ AI Assistant (ثابتة في كل جلسة)
- لا تعيد كتابة المعمارية بدون موافقة صريحة.
- لا تخلط UI logic مع Filesystem logic.
- لا Hashing جوه UI classes.
- لا وصول مباشر للـ DB من الـ UI.
- لا حذف ملفات من كود الـ Scanning.
- لا تحذف ملفات Temp قسراً إذا كانت محجوزة للبرامج (Skip on Lock).
- تحقق دائمًا من وجود مساحة كافية على بارتيشن الوجهة (D:) قبل تنفيذ النقل.
- حظر أي عمليات مسح أو نقل تطال ملفات ومجلدات النظام الحيوية على C:.
- Interfaces للمكونات القابلة للاستبدال.
- Unit Tests لكل خوارزمية ومسار تشغيلي.
- لا Dependencies زيادة عن الحاجة.
- لا AI/LLM functionality جوه التطبيق نفسه.
- لا Cloud services.
- مرحلة واحدة في كل مرة.

## 4. Critical Test Cases
| # | الحالة | المتوقع |
|---|---|---|
| 1 | ملفين نفس المحتوى | Group واحد |
| 2 | ملفين نفس الحجم، محتوى مختلف | مش Duplicates |
| 3 | نفس المحتوى، أسماء عربي/إنجليزي مختلطة | Group واحد |
| 4 | نفس الاسم، محتوى مختلف | مش Duplicates |
| 5 | ملف Inaccessible | السكان يكمل |
| 6 | ملف اتحذف أثناء السكان | تحذير + السكان يكمل |
| 7 | المستخدم عمل Cancel | وقف آمن، UI مستجيب |
| 8 | ملف Temp قيد التشغيل (Locked) | تخطي الملف وإكمال التنظيف دون انهيار |
| 9 | مساحة D: غير كافية لنقل ملفات من C: | رفض العملية وعرض تنبيه واضح قبل البدء |
| 10 | محاولة تحديد مجلد نظام مثل C:\Windows | حظر العملية فورياً بواسطة الـ Guard |

## 5. جدول تتبع التقدّم
| Gate/Phase | الحالة | ملاحظات |
|---|---|---|
| Gate 0 — Architecture Foundation | ✅ مكتمل (Completed) | التحقق من مطابقة الهيكل والـ Interfaces الحالية |
| Phase 1 — File Discovery | ✅ مكتمل (Completed) | FileScanner implemented |
| Phase 2 — Hash Engine | ✅ مكتمل (Completed) | HashService implemented |
| Phase 3 — Duplicate Engine | ✅ مكتمل (Completed) | DuplicateEngine implemented |
| Phase 4 — SQLite | ✅ مكتمل (Completed) | AppDbContext implemented |
| Phase 5A — Standard File Ops | ✅ مكتمل (Completed) | FileOperationService implemented |
| Phase 5B — Temp Cleanup Engine | ✅ مكتمل (Completed) | TempCleanupService implemented |
| Phase 5C — Drive Relocation Engine | ✅ مكتمل (Completed) | DriveRelocationService implemented |
| Phase 6 — UI Dashboard | ✅ مكتمل (Completed) | OverviewView wired to IFileScanner with cancellation support. Phase 6 UI fully implemented (2026-09-11). |
| Phase 7 — Similarity Framework (Extension Point) | ✅ مكتمل (Completed) | Extension point implemented. |
| Phase 8 — Image Analyzer | ✅ مكتمل (Completed) | dHash engine via ImageSharp implemented (2026-09-11). |
| Phase 12 — Similarity Integration | ✅ مكتمل (Completed) | Overview UI wired to SimilarityScannerService (2026-09-11). |
| Phase 13 — Settings Persistence & Deployment | ✅ مكتمل (Completed) | JSON settings caching and deployment prep (UI pending). |
| Phase 14 — UI/UX Refinements & Polish | ⏳ قيد التنفيذ (In Progress) | Dashboard fixes, Settings Browse button, Arabic localization, Dark Mode |
| Phase 9-11 — Video/Audio/Doc Analyzers | ❌ خارج نطاق MVP | |