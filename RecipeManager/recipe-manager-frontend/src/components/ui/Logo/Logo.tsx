// src/components/ui/Logo.tsx

import BlenderOutlinedIcon from '@mui/icons-material/BlenderOutlined';

export const Logo:React.FC = () => {  
    return (
        <>
            <BlenderOutlinedIcon sx={{ fontSize: 27 }}/>
            <h2>Recipe Manager</h2>
        </>
    )
}