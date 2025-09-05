# System Requirements Specification (SRS) for COGLyricsScanner

## 1. Introduction

### 1.1 Purpose
COGLyricsScanner is a .NET MAUI mobile application designed to digitize hymn lyrics from various physical hymn books in multiple languages. Using Plugin.Maui.OCR, the app will scan and extract text from printed hymnals, categorize them by source book, and store them in a local SQLite database (pcl-sqlite). Users can edit scanned results, manage existing lyrics, perform advanced searches, and export lyrics as formatted text.

The app is designed to run fully offline, ensuring portability, reliability, and independence from web services.

### 1.2 Scope
The app will:
- Scan hymn lyrics from physical books using OCR
- Categorize scanned lyrics by book and language
- Allow editing of newly scanned or existing lyrics
- Provide a searchable repository of all stored lyrics (homepage)
- Support exporting lyrics as TXT, DOCX, or PDF
- Run as a standalone .NET MAUI app with no web or shared project dependencies

## 2. Functional Requirements

### 2.1 Core Pages (3-Page Architecture)

#### Scan Page (OCR Page)
- **FR-001**: Capture lyrics using device camera
- **FR-002**: Use Plugin.Maui.OCR for text recognition
- **FR-003**: Support multiple OCR languages (English, Swahili, French, etc.)
- **FR-004**: Prompt user to select or create a Hymn Book Category
- **FR-005**: Save recognized text directly to the local SQLite database
- **FR-006**: Provide image preview before OCR processing
- **FR-007**: Allow manual text correction during scan process
- **FR-008**: Support batch scanning of multiple pages

#### Edit Page (Lyrics Editor)
- **FR-009**: Display scanned or existing lyrics in a rich text editor
- **FR-010**: Support text formatting (bold, italic, underline, line breaks)
- **FR-011**: Allow corrections and updates to lyrics
- **FR-012**: Enable adding metadata:
  - Hymn Book Name
  - Hymn Number
  - Title
  - Language
  - Tags (e.g., "Worship", "Communion")
- **FR-013**: Provide version history tracking for edited lyrics
- **FR-014**: Auto-save functionality during editing
- **FR-015**: Undo/Redo functionality
- **FR-016**: Word count and character count display

#### Homepage (Lyrics Repository & Search)
- **FR-017**: Display a categorized list of all stored lyrics
- **FR-018**: Provide multiple search and filter options:
  - Search by title, hymn number, keyword, or phrase
  - Filter by language, hymn book, or tags
- **FR-019**: Allow users to mark favorites and create collections (e.g., "Sunday Set")
- **FR-020**: Show recently scanned/edited lyrics
- **FR-021**: Display statistics (total hymns, books, languages)
- **FR-022**: Sort options (alphabetical, date added, most recent)
- **FR-023**: Quick access to frequently used hymns

### 2.2 Export & Sharing
- **FR-024**: Export individual or multiple lyrics to:
  - TXT (plain text)
  - DOCX (formatted text)
  - PDF (print-ready format)
- **FR-025**: Allow copy-to-clipboard functionality
- **FR-026**: Share via email, WhatsApp, or file system
- **FR-027**: Batch export functionality
- **FR-028**: Custom formatting options for exports
- **FR-029**: Print functionality for supported devices

### 2.3 Data Management
- **FR-030**: Store all lyrics, categories, and metadata in a local SQLite database (pcl-sqlite)
- **FR-031**: Ensure database supports:
  - CRUD operations (create, read, update, delete)
  - Indexing for fast search queries
- **FR-032**: Provide backup and restore functionality (local export/import of database)
- **FR-033**: Data validation and integrity checks
- **FR-034**: Database optimization and maintenance tools
- **FR-035**: Import functionality from external sources

### 2.4 User Experience
- **FR-036**: Offline-first operation (no internet required)
- **FR-037**: Simple 3-tab navigation (Scan | Edit | Home)
- **FR-038**: Dark mode & light mode themes
- **FR-039**: Multi-language UI support (English, Swahili, French, etc.)
- **FR-040**: Accessibility features (font size adjustment, high contrast)
- **FR-041**: Tutorial and help system
- **FR-042**: Settings and preferences management

## 3. Non-Functional Requirements

### 3.1 Performance
- **NFR-001**: OCR scan and recognition should complete in under 5 seconds for a single page on modern devices
- **NFR-002**: Search queries should return results within 2 seconds
- **NFR-003**: The database should support at least 20,000 hymns without degradation
- **NFR-004**: App startup time should be under 3 seconds
- **NFR-005**: Memory usage should not exceed 200MB during normal operation
- **NFR-006**: Battery consumption should be optimized for extended use

### 3.2 Reliability
- **NFR-007**: Auto-save scanned and edited lyrics to prevent data loss
- **NFR-008**: Error handling for failed OCR scans (retry options)
- **NFR-009**: Backup/export database option for device migration
- **NFR-010**: 99.9% uptime for offline functionality
- **NFR-011**: Graceful handling of low storage conditions
- **NFR-012**: Recovery mechanisms for corrupted data

### 3.3 Security
- **NFR-013**: Store lyrics securely in SQLite with basic encryption option
- **NFR-014**: No external service dependencies (fully offline)
- **NFR-015**: Secure handling of user data and preferences
- **NFR-016**: Protection against data tampering

### 3.4 Maintainability
- **NFR-017**: Modular code structure within .NET MAUI (no shared project)
- **NFR-018**: Clear separation of UI, OCR service, and database logic
- **NFR-019**: Unit test coverage for database and OCR functions
- **NFR-020**: Comprehensive logging and error reporting
- **NFR-021**: Code documentation and inline comments

### 3.5 Usability
- **NFR-022**: Intuitive user interface requiring minimal learning curve
- **NFR-023**: Consistent design patterns across all pages
- **NFR-024**: Support for various screen sizes and orientations
- **NFR-025**: Responsive touch interactions

### 3.6 Compatibility
- **NFR-026**: Support for Android 7.0+ and iOS 12.0+
- **NFR-027**: Compatibility with various camera resolutions
- **NFR-028**: Support for different device form factors (phones, tablets)

## 4. System Architecture

### 4.1 Technology Stack
- **Framework**: .NET MAUI (cross-platform mobile)
- **OCR Engine**: Plugin.Maui.OCR
- **Database**: pcl-sqlite (local storage)
- **Export**: System.IO APIs + OpenXML SDK (for DOCX) + iTextSharp or Syncfusion (for PDF)
- **UI Framework**: .NET MAUI native controls
- **Testing**: xUnit for unit testing, Appium for UI testing

### 4.2 Layered Design
- **UI Layer** (Scan Page, Edit Page, Home Page)
- **Business Logic Layer** (hymn management, search algorithms)
- **OCR Service Layer** (wrapping Plugin.Maui.OCR)
- **Database Layer** (pcl-sqlite, lyrics repository)
- **Export Layer** (formatting & file generation)
- **Utility Layer** (logging, configuration, helpers)

### 4.3 Data Model

#### Core Entities
- **Hymn**
  - ID (Primary Key)
  - Title
  - Number
  - Lyrics (Text)
  - Language
  - HymnBookId (Foreign Key)
  - CreatedDate
  - ModifiedDate
  - IsFavorite
  - Tags

- **HymnBook**
  - ID (Primary Key)
  - Name
  - Language
  - Publisher
  - Year
  - Description

- **Collection**
  - ID (Primary Key)
  - Name
  - Description
  - CreatedDate

- **HymnCollection** (Many-to-Many)
  - HymnId
  - CollectionId

### 4.4 Security Considerations
- Local data encryption using SQLite encryption extensions
- Secure file handling for exports and backups
- Input validation and sanitization
- Protection against SQL injection attacks

## 5. User Interface Requirements

### 5.1 Navigation Structure
- **Primary Navigation**: Bottom tab bar with three main sections
- **Secondary Navigation**: Context-specific navigation within each section
- **Search Interface**: Global search accessible from all pages

### 5.2 Design Guidelines
- **Material Design** principles for Android
- **Human Interface Guidelines** for iOS
- **Consistent color scheme** across light and dark themes
- **Accessibility compliance** (WCAG 2.1 AA)

## 6. Testing Requirements

### 6.1 Unit Testing
- Database operations (CRUD)
- OCR service functionality
- Search algorithms
- Export functionality

### 6.2 Integration Testing
- OCR to database workflow
- Export generation pipeline
- Cross-platform compatibility

### 6.3 User Acceptance Testing
- End-to-end scanning workflow
- Search and filter functionality
- Export and sharing features

## 7. Deployment and Distribution

### 7.1 Platform Requirements
- **Android**: Google Play Store distribution
- **iOS**: Apple App Store distribution
- **Minimum OS versions**: Android 7.0, iOS 12.0

### 7.2 Installation Requirements
- **Storage**: Minimum 100MB free space
- **RAM**: Minimum 2GB
- **Camera**: Required for OCR functionality
- **Permissions**: Camera access, file system access

## 8. Maintenance and Support

### 8.1 Update Strategy
- Regular updates for bug fixes and performance improvements
- Feature updates based on user feedback
- Security patches as needed

### 8.2 Monitoring
- Crash reporting and analytics
- Performance monitoring
- User feedback collection

## 9. Constraints and Assumptions

### 9.1 Constraints
- Must work completely offline
- Limited to mobile platforms (Android/iOS)
- OCR accuracy dependent on image quality
- Storage limited by device capacity

### 9.2 Assumptions
- Users have basic smartphone operation knowledge
- Device cameras are functional and of reasonable quality
- Users will primarily scan printed text (not handwritten)
- Hymn books have standard text formatting

## 10. Glossary

- **OCR**: Optical Character Recognition - technology to convert images of text into machine-readable text
- **SQLite**: Lightweight, file-based database engine
- **.NET MAUI**: Multi-platform App UI framework for cross-platform development
- **Hymnal**: A book of hymns or religious songs
- **Metadata**: Data that provides information about other data
- **CRUD**: Create, Read, Update, Delete operations

---

**Document Version**: 1.0  
**Last Updated**: 5th Sept, 2025  
**Prepared By**: Eliud Amukambwa  
**Approved By**: Eliud Amukambwa