#import <AppKit/AppKit.h>
#import <objc/message.h>
#include <dispatch/dispatch.h>
#include <stdlib.h>
#include <string.h>

// GameAssembly.dylib does not link AppKit. Emit ld flags from this .o,
// and resolve panel / UTType classes at runtime so OBJC_CLASS_$_NS* are not required.
asm(".linker_option \"-framework\", \"AppKit\"");
asm(".linker_option \"-framework\", \"UniformTypeIdentifiers\"");

static id CreateAppKitPanel(NSString* className, SEL factorySelector)
{
    Class cls = NSClassFromString(className);
    if (cls == Nil)
    {
        NSLog(@"[StandaloneMacFileBrowser] Class %@ not found", className);
        return nil;
    }

    return ((id (*)(Class, SEL))objc_msgSend)(cls, factorySelector);
}

static void ActivateNsApp(void)
{
    Class appClass = NSClassFromString(@"NSApplication");
    if (appClass == Nil)
    {
        return;
    }

    id app = ((id (*)(Class, SEL))objc_msgSend)(appClass, @selector(sharedApplication));
    if (app == nil)
    {
        return;
    }

    ((void (*)(id, SEL, BOOL))objc_msgSend)(app, @selector(activateIgnoringOtherApps:), YES);
}

static char* RunOnMainThread(char* (^work)(void))
{
    if ([NSThread isMainThread])
    {
        return work();
    }

    __block char* result = NULL;
    dispatch_sync(dispatch_get_main_queue(), ^{
        result = work();
    });
    return result;
}

static char* CopyNSString(NSString* str)
{
    if (str == nil)
    {
        return NULL;
    }

    const char* utf8 = [str UTF8String];
    if (utf8 == NULL)
    {
        return NULL;
    }

    size_t length = strlen(utf8);
    char* copy = (char*)malloc(length + 1);
    if (copy == NULL)
    {
        return NULL;
    }

    memcpy(copy, utf8, length + 1);
    return copy;
}

static NSArray<NSString*>* ParseExtensions(const char* extensions)
{
    if (extensions == NULL || extensions[0] == '\0')
    {
        return nil;
    }

    NSString* joined = [NSString stringWithUTF8String:extensions];
    NSCharacterSet* separators = [NSCharacterSet characterSetWithCharactersInString:@",; "];
    NSArray<NSString*>* parts = [joined componentsSeparatedByCharactersInSet:separators];

    NSMutableArray<NSString*>* result = [NSMutableArray array];
    for (NSString* part in parts)
    {
        NSString* trimmed = [part stringByTrimmingCharactersInSet:[NSCharacterSet whitespaceCharacterSet]];
        if (trimmed.length == 0)
        {
            continue;
        }

        if ([trimmed hasPrefix:@"*."])
        {
            trimmed = [trimmed substringFromIndex:2];
        }
        else if ([trimmed hasPrefix:@"."])
        {
            trimmed = [trimmed substringFromIndex:1];
        }

        if (trimmed.length > 0)
        {
            [result addObject:trimmed];
        }
    }

    return result.count > 0 ? result : nil;
}

static void ApplyAllowedContentTypes(NSSavePanel* panel, NSArray<NSString*>* types)
{
    if (types == nil)
    {
        return;
    }

    Class utTypeClass = NSClassFromString(@"UTType");
    SEL typeWithFilenameExtension = NSSelectorFromString(@"typeWithFilenameExtension:");
    if (utTypeClass != Nil && [utTypeClass respondsToSelector:typeWithFilenameExtension])
    {
        NSMutableArray* contentTypes = [NSMutableArray array];
        for (NSString* ext in types)
        {
            id utType = ((id (*)(Class, SEL, NSString*))objc_msgSend)(
                utTypeClass,
                typeWithFilenameExtension,
                ext);
            if (utType != nil)
            {
                [contentTypes addObject:utType];
            }
        }

        if (contentTypes.count > 0)
        {
            ((void (*)(id, SEL, NSArray*))objc_msgSend)(panel, @selector(setAllowedContentTypes:), contentTypes);
            return;
        }
    }

#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Wdeprecated-declarations"
    panel.allowedFileTypes = types;
#pragma clang diagnostic pop
}

static char* OpenFilePanelOnMain(const char* title, const char* directory, const char* extensions)
{
    @autoreleasepool
    {
        NSOpenPanel* panel = CreateAppKitPanel(@"NSOpenPanel", @selector(openPanel));
        if (panel == nil)
        {
            return NULL;
        }

        panel.canChooseFiles = YES;
        panel.canChooseDirectories = NO;
        panel.allowsMultipleSelection = NO;
        panel.resolvesAliases = YES;

        if (title != NULL && title[0] != '\0')
        {
            panel.title = [NSString stringWithUTF8String:title];
        }

        if (directory != NULL && directory[0] != '\0')
        {
            panel.directoryURL = [NSURL fileURLWithPath:[NSString stringWithUTF8String:directory] isDirectory:YES];
        }

        ApplyAllowedContentTypes(panel, ParseExtensions(extensions));

        ActivateNsApp();
        NSModalResponse response = [panel runModal];
        if (response != NSModalResponseOK || panel.URL == nil)
        {
            return NULL;
        }

        return CopyNSString(panel.URL.path);
    }
}

static char* SaveFilePanelOnMain(
    const char* title,
    const char* directory,
    const char* defaultName,
    const char* extension)
{
    @autoreleasepool
    {
        NSSavePanel* panel = CreateAppKitPanel(@"NSSavePanel", @selector(savePanel));
        if (panel == nil)
        {
            return NULL;
        }

        panel.canCreateDirectories = YES;
        panel.showsTagField = NO;
        panel.allowsOtherFileTypes = NO;

        if (title != NULL && title[0] != '\0')
        {
            panel.title = [NSString stringWithUTF8String:title];
        }

        if (directory != NULL && directory[0] != '\0')
        {
            panel.directoryURL = [NSURL fileURLWithPath:[NSString stringWithUTF8String:directory] isDirectory:YES];
        }

        if (defaultName != NULL && defaultName[0] != '\0')
        {
            panel.nameFieldStringValue = [NSString stringWithUTF8String:defaultName];
        }

        ApplyAllowedContentTypes(panel, ParseExtensions(extension));

        ActivateNsApp();
        NSModalResponse response = [panel runModal];
        if (response != NSModalResponseOK || panel.URL == nil)
        {
            return NULL;
        }

        return CopyNSString(panel.URL.path);
    }
}

extern "C" {

void kknngggg_StandaloneMacFileBrowser_Free(char* ptr)
{
    free(ptr);
}

char* kknngggg_StandaloneMacFileBrowser_OpenFilePanel(const char* title, const char* directory, const char* extensions)
{
    return RunOnMainThread(^{
        return OpenFilePanelOnMain(title, directory, extensions);
    });
}

char* kknngggg_StandaloneMacFileBrowser_SaveFilePanel(
    const char* title,
    const char* directory,
    const char* defaultName,
    const char* extension)
{
    return RunOnMainThread(^{
        return SaveFilePanelOnMain(title, directory, defaultName, extension);
    });
}

}
