import styles from './SearchBar.module.css';
import SearchIcon from '@mui/icons-material/Search';

interface SearchBarProps {
    searchQuery: string;
    onSearchChange: (query: string) => void;
    placeholder? : string;
}

export const SearchBar: React.FC<SearchBarProps> = ({
    searchQuery,
    onSearchChange,
    placeholder = "Search recipes..."
}) => {

    const handleInputChange = (e:React.ChangeEvent<HTMLInputElement>) => {
        onSearchChange(e.target.value);
        console.log(searchQuery);
    } 

    return (
        <div className={styles.searchContainer}>
            <div className={styles.searchInputWrapper}>
                <SearchIcon className={styles.searchIcon} />

                <input
                    type="text"
                    value={searchQuery}
                    onChange={handleInputChange}
                    placeholder={placeholder}
                    className={styles.searchInput}
                    aria-label="Search recipes"
                />
            </div>
        </div >
    );
}