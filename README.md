# NuGet Package Manager

A comprehensive .NET 10 WinForms application for batch management of NuGet packages, including querying, deprecating, and unlisting operations.

## Features

### 🔍 Package Version Query
- **Complete Version Discovery**: Uses enhanced NuGet.org API with intelligent fallback mechanisms
- **Registration API + Search API**: Ensures all package versions are discovered, including both listed and unlisted
- **Unlisted Status**: Clear visual indication of package status in DataGridView
- **Smart Pagination**: Handles API pagination automatically to fetch all versions

### 📋 Version Selection
- **Multi-Select Support**: Individual version selection via checkboxes
- **Bulk Selection Options**:
  - Select All versions
  - Select only Listed versions
  - Select only Unlisted versions
- **Dynamic Button Updates**: Button text changes based on selection (e.g., "Unlist (3)" or "Smart List/Unlist (2→Unlist, 1→Note)")

### 🗑️ Deprecation Features
- **True Deprecation Attempt**: First tries NuGet.org API for real deprecation
- **Fallback to Unlist**: If API deprecation unavailable, falls back to unlist operation
- **Deprecation Reasons**:
  - Critical Bugs
  - Legacy
  - Other (with custom message)
- **Alternative Package Suggestion**: Option to specify replacement package and version
- **Detailed Logging**: All deprecation actions and reasons are logged

### 📝 Smart Unlist Operations
- **Intelligent Operation**: 
  - Listed versions → Unlisted (using `nuget delete`)
- **Batch Processing**: Process multiple versions efficiently
- **Clear Status Feedback**: Real-time progress and detailed operation logs

### 🔄 Async Operations & UX
- **Non-blocking UI**: All operations run asynchronously
- **Progress Tracking**: Visual progress bar and status updates
- **Cancellation Support**: Cancel long-running operations
- **Auto-refresh**: Version list automatically refreshes after operations
- **Loading States**: Clear loading indicators during API calls

### 📊 Logging & Monitoring
- **Dockable Log Window**: Auto-docks to the right side of main window
- **Real-time Logging**: Live operation progress and results
- **Detailed Command Output**: Shows actual NuGet CLI commands and responses
- **Error Handling**: Comprehensive error capture and reporting
- **Log Management**: Clear logs, close window functionality

### ⚙️ Configuration
- **API Key Support**: Secure API key input for NuGet.org operations
- **Local NuGet CLI**: Uses bundled nuget.exe from `tools/` directory
- **Responsive UI**: Auto-scaling interface elements

## Recent Improvements

### Version Query Enhancement
- **Enhanced API Coverage**: Improved algorithm to discover all package versions
- **Better Error Handling**: Graceful fallback when API endpoints fail
- **Deduplication**: Automatic removal of duplicate versions from multiple API sources

### Deprecation System Overhaul
- **API-First Approach**: Attempts true deprecation via NuGet.org API
- **Rich Metadata**: Captures deprecation reasons and alternative package info
- **Fallback Strategy**: Uses unlist when API deprecation is unavailable

### Smart Operations
- **Context-Aware Buttons**: Button text updates based on selected version states
- **Intelligent Processing**: Different actions for listed vs unlisted versions
- **User Guidance**: Clear instructions for operations that require manual steps

### Enhanced Logging
- **Operation Context**: Each log entry includes operation type and version
- **Status Symbols**: Uses ✓, ✗, ⚠️ symbols for quick status recognition
- **Command Visibility**: Shows actual CLI commands (with API key masked)

## Usage

1. **Enter Package Name**: Type the NuGet package ID
2. **Search Versions**: Click "Search" to discover all versions
3. **Select Versions**: Use checkboxes or bulk selection buttons
4. **Choose Operation**:
   - **Deprecate**: Set deprecation with reasons and alternatives
   - **Smart List/Unlist**: Intelligently unlist listed versions or show re-list guidance
5. **Monitor Progress**: Watch real-time progress in the docked log window
6. **Review Results**: Check detailed logs for operation outcomes

## API Key Requirements

- Required for all write operations (deprecate, unlist)
- Obtain from [NuGet.org API Keys](https://www.nuget.org/account/apikeys)
- Stored temporarily during session (not persisted)

## Technical Notes

- **Framework**: .NET 10 WinForms
- **APIs Used**: NuGet.org Registration API v3, Search API, Package API
- **CLI Tool**: Bundled nuget.exe for reliable operations
- **Async Pattern**: All network operations use async/await with cancellation support

## Limitations

- **Re-listing**: Unlisted packages cannot be re-listed via API; requires re-publishing
- **API Deprecation**: NuGet.org may not support full deprecation API; falls back to unlist
- **API Rate Limits**: Subject to NuGet.org API rate limiting

## Future Enhancements

- Integration with package publishing workflow
- Bulk package management across multiple packages
- Enhanced deprecation metadata support
- Package ownership and collaboration features
