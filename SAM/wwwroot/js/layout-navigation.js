(function() {
            const sidebar = document.getElementById('sidebar');
            const sidebarToggle = document.getElementById('sidebarToggle');
            const sidebarCollapseToggle = document.getElementById('sidebarCollapseToggle');
            const overlay = document.getElementById('sidebarOverlay');
            const mainContent = document.getElementById('mainContent');
            const navLinks = document.querySelectorAll('.sidebar-nav .nav-link');
            
            const DESKTOP_COLLAPSED_KEY = 'sidebarCollapsed';
            const MOBILE_OPEN_KEY = 'sidebarMobileOpen';

            function isDesktopViewport() {
                return window.innerWidth >= 768;
            }

            function setMobileSidebarState(isOpen) {
                if (!sidebar || !mainContent) return;

                sidebar.classList.toggle('collapsed', !isOpen);
                overlay?.classList.toggle('show', isOpen);
                mainContent.classList.toggle('sidebar-collapsed', !isOpen);
                localStorage.setItem(MOBILE_OPEN_KEY, isOpen ? 'true' : 'false');
            }

            function setDesktopSidebarState(isCollapsed) {
                if (!sidebar || !mainContent) return;

                sidebar.classList.toggle('sidebar-collapsed-desktop', isCollapsed);
                mainContent.classList.toggle('sidebar-collapsed-desktop', isCollapsed);
                sidebarCollapseToggle?.setAttribute('aria-expanded', isCollapsed ? 'false' : 'true');
                localStorage.setItem(DESKTOP_COLLAPSED_KEY, isCollapsed ? 'true' : 'false');
            }

            function applySidebarState() {
                if (!sidebar || !mainContent) return;

                if (isDesktopViewport()) {
                    const desktopCollapsed = localStorage.getItem(DESKTOP_COLLAPSED_KEY) === 'true';

                    sidebar.classList.remove('collapsed');
                    overlay?.classList.remove('show');
                    mainContent.classList.remove('sidebar-collapsed');
                    setDesktopSidebarState(desktopCollapsed);
                } else {
                    const mobileOpen = localStorage.getItem(MOBILE_OPEN_KEY) === 'true';

                    sidebar.classList.remove('sidebar-collapsed-desktop');
                    mainContent.classList.remove('sidebar-collapsed-desktop');
                    sidebarCollapseToggle?.setAttribute('aria-expanded', 'true');
                    setMobileSidebarState(mobileOpen);
                }
            }

            if (localStorage.getItem(MOBILE_OPEN_KEY) === null) {
                localStorage.setItem(MOBILE_OPEN_KEY, 'false');
            }

            applySidebarState();
            
            // Mobile sidebar toggle
            sidebarToggle?.addEventListener('click', function() {
                const isOpen = !sidebar?.classList.contains('collapsed');
                setMobileSidebarState(!isOpen);
            });
            
            // Desktop sidebar collapse/expand toggle
            sidebarCollapseToggle?.addEventListener('click', function() {
                const isCollapsed = sidebar?.classList.contains('sidebar-collapsed-desktop');
                setDesktopSidebarState(!isCollapsed);
            });
            
            // Close sidebar when overlay is clicked (mobile)
            overlay?.addEventListener('click', function() {
                setMobileSidebarState(false);
            });
            
            // Dropdown functionality
            const dropdownItems = document.querySelectorAll('.nav-dropdown-item');
            const dropdownToggles = document.querySelectorAll('.nav-dropdown-toggle');
            
            // Toggle dropdown
            function toggleDropdown(item, forceState) {
                const toggle = item.querySelector('.nav-dropdown-toggle');
                const isExpanded = item.classList.contains('expanded');
                const newState = forceState !== undefined ? forceState : !isExpanded;

                if (newState) {
                    dropdownItems.forEach(otherItem => {
                        if (otherItem !== item) {
                            otherItem.classList.remove('expanded');
                            const otherToggle = otherItem.querySelector('.nav-dropdown-toggle');
                            otherToggle?.setAttribute('aria-expanded', 'false');

                            if (!otherItem.querySelector('.nav-link.active')) {
                                otherItem.classList.remove('has-active');
                            }
                        }
                    });
                }
                
                if (newState) {
                    item.classList.add('expanded');
                    toggle?.setAttribute('aria-expanded', 'true');
                } else {
                    item.classList.remove('expanded');
                    toggle?.setAttribute('aria-expanded', 'false');
                }
            }
            
            // Initialize dropdown toggles
            dropdownToggles.forEach(toggle => {
                toggle.addEventListener('click', function(e) {
                    e.preventDefault();
                    e.stopPropagation();
                    const item = this.closest('.nav-dropdown-item');
                    if (item) {
                        toggleDropdown(item);
                    }
                });
                
                // Keyboard support
                toggle.addEventListener('keydown', function(e) {
                    if (e.key === 'Enter' || e.key === ' ') {
                        e.preventDefault();
                        const item = this.closest('.nav-dropdown-item');
                        if (item) {
                            toggleDropdown(item);
                        }
                    }
                });
            });
            
            // Enhanced active link detection with dropdown support
            function setActiveNavLink() {
                const currentPath = window.location.pathname.toLowerCase();
                const currentUrl = new URL(window.location.href);
                let activeDropdown = null;
                
                navLinks.forEach(link => {
                    const href = link.getAttribute('href');
                    if (!href) return;
                    
                    try {
                        const linkUrl = new URL(href, window.location.origin);
                        const linkPath = linkUrl.pathname.toLowerCase();
                        
                        // Extract controller and action from paths for more precise matching
                        const currentParts = currentPath.split('/').filter(p => p);
                        const linkParts = linkPath.split('/').filter(p => p);
                        
                        let isActive = false;
                        
                        // Exact path match
                        if (linkPath === currentPath) {
                            isActive = true;
                        }
                        // For SystemAdmin, check if controller and action match (ignore query params)
                        else if (currentPath.includes('/systemadmin/systemadmin') && 
                                linkPath.includes('/systemadmin/systemadmin')) {
                            isActive = true;
                        }
                        // For paths with same controller, check if action matches exactly
                        else if (currentParts.length > 0 && linkParts.length > 0) {
                            const currentController = currentParts[0];
                            const currentAction = currentParts.length > 1 ? currentParts[1] : 'index';
                            const linkController = linkParts[0];
                            const linkAction = linkParts.length > 1 ? linkParts[1] : 'index';
                            
                            // If controllers match, actions must match exactly
                            if (currentController === linkController) {
                                // Both paths have the same controller, so actions must match exactly
                                if (currentAction === linkAction) {
                                    isActive = true;
                                }
                            }
                        }
                        // Fallback: check if current path starts with link path (for parent paths)
                        else if (currentPath.startsWith(linkPath) && linkPath !== '/' && 
                                (!currentPath[linkPath.length] || currentPath[linkPath.length] === '/')) {
                            // Only allow this for paths that are truly parent paths
                            // Exclude cases where both share the same controller but different actions
                            if (currentParts.length > 0 && linkParts.length > 0) {
                                const currentController = currentParts[0];
                                const linkController = linkParts[0];
                                // Only allow startsWith if controllers are different
                                if (currentController !== linkController) {
                                    isActive = true;
                                }
                            } else {
                                isActive = true;
                            }
                        }
                        
                        if (isActive) {
                            link.classList.add('active');
                            link.setAttribute('aria-current', 'page');
                            
                            // Find parent dropdown and expand it
                            const dropdownItem = link.closest('.nav-dropdown-item');
                            if (dropdownItem) {
                                activeDropdown = dropdownItem;
                                dropdownItem.classList.add('has-active');
                                toggleDropdown(dropdownItem, true);
                            }
                        } else {
                            link.classList.remove('active');
                            link.removeAttribute('aria-current');
                        }
                    } catch (e) {
                        // Fallback to simple path comparison if URL parsing fails
                        const linkPath = href.toLowerCase().split('?')[0];
                        const currentParts = currentPath.split('/').filter(p => p);
                        const linkParts = linkPath.split('/').filter(p => p);
                        
                        let isActive = false;
                        
                        if (linkPath === currentPath) {
                            isActive = true;
                        } else if (currentParts.length > 0 && linkParts.length > 0) {
                            const currentController = currentParts[0];
                            const currentAction = currentParts.length > 1 ? currentParts[1] : 'index';
                            const linkController = linkParts[0];
                            const linkAction = linkParts.length > 1 ? linkParts[1] : 'index';
                            
                            if (currentController === linkController) {
                                // Both paths have the same controller, so actions must match exactly
                                if (currentAction === linkAction) {
                                    isActive = true;
                                }
                            }
                        }
                        
                        if (isActive) {
                            link.classList.add('active');
                            link.setAttribute('aria-current', 'page');
                            
                            const dropdownItem = link.closest('.nav-dropdown-item');
                            if (dropdownItem) {
                                activeDropdown = dropdownItem;
                                dropdownItem.classList.add('has-active');
                                toggleDropdown(dropdownItem, true);
                            }
                        } else {
                            link.classList.remove('active');
                            link.removeAttribute('aria-current');
                        }
                    }
                });
                
                // Remove has-active from other dropdowns
                dropdownItems.forEach(item => {
                    if (item !== activeDropdown) {
                        item.classList.remove('has-active');
                        toggleDropdown(item, false);
                    }
                });
            }
            
            // Set active nav link on page load
            setActiveNavLink();
            
            // Enhanced keyboard navigation (includes dropdowns)
            let currentFocusIndex = -1;
            
            function getFocusableElements() {
                const elements = [];
                
                // Add regular nav links
                navLinks.forEach(link => {
                    const style = window.getComputedStyle(link);
                    if (style.display !== 'none' && style.visibility !== 'hidden') {
                        elements.push(link);
                    }
                });
                
                // Add dropdown toggles
                dropdownToggles.forEach(toggle => {
                    const style = window.getComputedStyle(toggle);
                    if (style.display !== 'none' && style.visibility !== 'hidden') {
                        elements.push(toggle);
                    }
                });
                
                return elements;
            }
            
            function focusElement(index) {
                const focusableElements = getFocusableElements();
                if (focusableElements.length === 0) return;
                
                currentFocusIndex = Math.max(0, Math.min(index, focusableElements.length - 1));
                focusableElements[currentFocusIndex].focus();
            }
            
            sidebar?.addEventListener('keydown', function(e) {
                const focusableElements = getFocusableElements();
                if (focusableElements.length === 0) return;
                
                // Find current focused element index
                const currentFocused = focusableElements.indexOf(document.activeElement);
                if (currentFocused === -1) {
                    currentFocusIndex = 0;
                } else {
                    currentFocusIndex = currentFocused;
                }
                
                const activeElement = document.activeElement;
                const isDropdownToggle = activeElement?.classList.contains('nav-dropdown-toggle');
                
                switch(e.key) {
                    case 'ArrowDown':
                        e.preventDefault();
                        if (isDropdownToggle && !activeElement.closest('.nav-dropdown-item')?.classList.contains('expanded')) {
                            // Expand dropdown if collapsed
                            const item = activeElement.closest('.nav-dropdown-item');
                            if (item) {
                                toggleDropdown(item, true);
                            }
                        } else {
                            focusElement(currentFocusIndex + 1);
                        }
                        break;
                    case 'ArrowUp':
                        e.preventDefault();
                        focusElement(currentFocusIndex - 1);
                        break;
                    case 'ArrowRight':
                        e.preventDefault();
                        if (isDropdownToggle) {
                            const item = activeElement.closest('.nav-dropdown-item');
                            if (item && !item.classList.contains('expanded')) {
                                toggleDropdown(item, true);
                            }
                        }
                        break;
                    case 'ArrowLeft':
                        e.preventDefault();
                        if (isDropdownToggle) {
                            const item = activeElement.closest('.nav-dropdown-item');
                            if (item && item.classList.contains('expanded')) {
                                toggleDropdown(item, false);
                            }
                        }
                        break;
                    case 'Home':
                        e.preventDefault();
                        focusElement(0);
                        break;
                    case 'End':
                        e.preventDefault();
                        focusElement(focusableElements.length - 1);
                        break;
                    case 'Escape':
                        // Close mobile sidebar on Escape
                        if (window.innerWidth < 768) {
                            setMobileSidebarState(false);
                        } else {
                            // Close all dropdowns on Escape
                            dropdownItems.forEach(item => {
                                if (item.classList.contains('expanded')) {
                                    toggleDropdown(item, false);
                                }
                            });
                        }
                        break;
                }
            });
            
            // Close dropdowns when clicking outside (for collapsed sidebar)
            document.addEventListener('click', function(e) {
                if (sidebar?.classList.contains('sidebar-collapsed-desktop')) {
                    const isClickInsideDropdown = e.target.closest('.nav-dropdown-item');
                    if (!isClickInsideDropdown) {
                        dropdownItems.forEach(item => {
                            if (item.classList.contains('expanded')) {
                                toggleDropdown(item, false);
                            }
                        });
                    }
                }
            });
            
            // Handle window resize
            let resizeTimer;
            window.addEventListener('resize', function() {
                clearTimeout(resizeTimer);
                resizeTimer = setTimeout(function() {
                    applySidebarState();
                }, 150);
            });
        })();
