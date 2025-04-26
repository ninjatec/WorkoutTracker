/**
 * Module loader for WorkoutTracker
 * Handles dynamic loading of JavaScript modules based on page needs
 */
(function() {
    // Object to store module definitions
    const modules = {};
    
    // Pages that require specific modules
    const pageModules = {
        '/': ['home'],
        '/index': ['home'],
        '/sessions/index': ['sessions-list'],
        '/sessions/create': ['session-editor'],
        '/sessions/edit': ['session-editor'],
        '/sessions/details': ['session-details'],
        '/reports/index': ['charts', 'reports'],
        '/calculator/onerepmax': ['calculator'],
        '/dataporability/import': ['import-export'],
        '/dataporability/export': ['import-export'],
        '/dataporability/importtrainai': ['import-export']
    };
    
    // Base path for modules
    const modulesBasePath = '/js/modules/';
    
    /**
     * Register a module
     * @param {string} name - Module name
     * @param {Function} initFn - Module initialization function
     * @param {Array} dependencies - Module dependencies
     */
    window.registerModule = function(name, initFn, dependencies = []) {
        modules[name] = {
            init: initFn,
            dependencies: dependencies,
            initialized: false
        };
    };
    
    /**
     * Initialize a module and its dependencies
     * @param {string} name - Module name to initialize
     * @returns {Promise} - Promise that resolves when the module is initialized
     */
    window.initModule = async function(name) {
        if (!modules[name]) {
            await loadModuleScript(name);
        }
        
        const module = modules[name];
        
        if (module.initialized) {
            return Promise.resolve();
        }
        
        // Initialize dependencies first
        const dependencyPromises = module.dependencies.map(dep => initModule(dep));
        await Promise.all(dependencyPromises);
        
        // Initialize this module
        await module.init();
        module.initialized = true;
        
        return Promise.resolve();
    };
    
    /**
     * Load a module script dynamically
     * @param {string} name - Module name
     * @returns {Promise} - Promise that resolves when the script is loaded
     */
    function loadModuleScript(name) {
        return new Promise((resolve, reject) => {
            const script = document.createElement('script');
            script.src = `${modulesBasePath}${name}.js${getCacheBuster()}`;
            script.async = true;
            script.onload = resolve;
            script.onerror = reject;
            document.head.appendChild(script);
        });
    }
    
    /**
     * Get cache buster string for development
     * @returns {string} - Cache buster query string
     */
    function getCacheBuster() {
        // Only use in development
        if (window.location.hostname === 'localhost') {
            return `?_=${new Date().getTime()}`;
        }
        return '';
    }
    
    /**
     * Initialize modules for current page
     */
    function initCurrentPageModules() {
        const path = window.location.pathname.toLowerCase();
        let modulesToLoad = [];
        
        // Find exactly matching paths
        if (pageModules[path]) {
            modulesToLoad = pageModules[path];
        } else {
            // Check for partial matches
            Object.entries(pageModules).forEach(([pagePath, modules]) => {
                if (path.includes(pagePath) && pagePath !== '/') {
                    modulesToLoad = [...modulesToLoad, ...modules];
                }
            });
        }
        
        // Load common modules for all pages
        modulesToLoad.push('common');
        
        // Remove duplicates
        modulesToLoad = [...new Set(modulesToLoad)];
        
        // Initialize each module
        modulesToLoad.forEach(module => {
            initModule(module).catch(err => {
                console.error(`Failed to initialize module ${module}:`, err);
            });
        });
    }
    
    // Initialize on DOMContentLoaded
    document.addEventListener('DOMContentLoaded', initCurrentPageModules);
})();